#!/bin/bash

# --- Function for Error Handling ---
# Usage: check_error "Error message if the previous command failed"
check_error() {
  local exit_code=$? # Capture the exit code of the previous command
  if [ $exit_code -ne 0 ]; then
    # Print the error message passed as an argument to standard error
    printf "\n\t❌ Error: %s (Exit Code: %d)\n" "$1" $exit_code >&2
    # Optional: Add cleanup here if needed before exiting
    # rm -rf tmp # Example cleanup
    exit $exit_code # Exit the script with the same error code
  fi
}

# --- Initial Setup ---
temp_dir="tmp"
output_xapk="modified.xapk"
modified_dll="modified_assembly.dll"
target_dll_path="apk/assets/bin/Data/Managed/Assembly-CSharp.dll" # Relative path within temp_dir
keystore_path="/root/.android/debug.keystore"

printf "[*] Starting XAPK modification process...\n"

# --- Find Input XAPK ---
printf "[*] Searching for input XAPK file...\n"
shopt -s nullglob
input_xapk_files=(*.xapk)
shopt -u nullglob

if [ ${#input_xapk_files[@]} -ne 1 ]; then
    printf "\t❌ Error: Expected exactly one .xapk file in the current directory, found %d.\n" "${#input_xapk_files[@]}" >&2
    exit 1
fi
input_xapk="${input_xapk_files[0]}"
printf "\t[*] Found input XAPK: %s\n" "$input_xapk"

# --- Create Temporary Directory ---
if [ -d "$temp_dir" ]; then
    printf "\t⚠️ Warning: Temporary directory '%s' already exists. Removing it.\n" "$temp_dir" >&2
    rm -rf "$temp_dir"
    check_error "Failed to remove existing temporary directory '$temp_dir'."
fi
printf "[*] Creating temporary directory: %s\n" "$temp_dir"
mkdir "$temp_dir"
check_error "Failed to create temporary directory '$temp_dir'."

# --- Unzip XAPK ---
printf "[*] Unzipping XAPK '%s' into '%s'...\n" "$input_xapk" "$temp_dir"
unzip -q "$input_xapk" -d "$temp_dir" # Use -q for quieter unzip
check_error "Failed to unzip '$input_xapk'."

# --- Find and Decompile APK ---
printf "[*] Searching for APK within '%s'...\n" "$temp_dir"
shopt -s nullglob
apk_files=("$temp_dir"/*.apk)
shopt -u nullglob

if [ ${#apk_files[@]} -ne 1 ]; then
    printf "\t❌ Error: Expected exactly one .apk file inside '%s', found %d.\n" "$temp_dir" "${#apk_files[@]}" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
apk_file="${apk_files[0]}"
apk_basename=$(basename "$apk_file") # Get just the filename
printf "\t[*] Found APK: %s\n" "$apk_basename"

printf "[*] Decompiling APK '%s' using apktool...\n" "$apk_basename"
apktool d "$apk_file" -f -o "$temp_dir/apk" # Use -f to force overwrite if apk dir exists
check_error "apktool failed to decompile '$apk_file'."

# --- Remove Original APK ---
printf "[*] Removing original APK file '%s'...\n" "$apk_basename"
rm -f "$apk_file" # Use -f to ignore if already gone
check_error "Failed to remove original APK file '$apk_file'."

# --- Run Custom Modification Step ---
printf "[*] Running custom modification step (dotnet run)...\n"
cp "unmodified/bin/Assembly-CSharp.dll" "monocecil/to_mod.dll"
cp mods.cs "monocecil/mods.cs"
cd monocecil
# Assuming 'dotnet run' operates in the current directory and produces modified_assembly.dll
dotnet run
rm -r mods.cs to_mod.dll
check_error "'dotnet run' command failed. Check its output for details."

# --- Verify Custom Step Output ---
printf "[*] Verifying output of custom step...\n"
if [ ! -f "$modified_dll" ]; then
    printf "\t❌ Error: Expected output file '%s' was not created by 'dotnet run'.\n" "$modified_dll" >&2
    
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
printf "\t[*] Found modified DLL: %s\n" "$modified_dll"

# --- Disassemble Modified DLL (Using ikdasm) ---
printf "[*] Disassembling '%s' with ikdasm...\n" "$modified_dll"
il_output_file="${modified_dll%.dll}.il" # e.g., modified_assembly.il
ikdasm "$modified_dll" > "$il_output_file"
check_error "ikdasm failed for '$modified_dll'."

# --- Verify Disassembly Output ---
if [ ! -s "$il_output_file" ]; then # Check if file exists and is not empty
    printf "\t❌ Error: ikdasm ran but output file '%s' was not created or is empty.\n" "$il_output_file" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
mv "$il_output_file" "../$il_output_file"
printf "\t[*] Disassembly saved to: %s\n" "$il_output_file"


# --- Replace Original DLL ---
target_dll_full_path="$target_dll_path"
target_dll_dir=$(dirname "$target_dll_full_path")

printf "[*] Replacing target DLL '%s'...\n" "$target_dll_path"
# Check if target directory exists

if [ ! -d "../$temp_dir/apk/assets/bin/Data/Managed/" ]; then
    printf "\t❌ Error: Target directory '%s' for DLL replacement does not exist.\n" "$temp_dir/$target_dll_full_path" >&2
    rm -rf "../$temp_dir" # Clean up temp dir
    exit 1
fi
mv "$modified_dll" "../$temp_dir/$target_dll_full_path"
check_error "Failed to move '$modified_dll' to '$target_dll_full_path'."
printf "\t[*] Replaced DLL successfully.\n"
cd ..

# --- Rebuild APK ---
rebuilt_apk="$temp_dir/modified.apk"
printf "[*] Rebuilding APK using apktool...\n"
apktool b "$temp_dir/apk" -o "$rebuilt_apk"
check_error "apktool failed to rebuild the APK."

# --- Verify Rebuilt APK ---
if [ ! -s "$rebuilt_apk" ]; then
    printf "\t❌ Error: apktool ran but output file '%s' was not created or is empty.\n" "$rebuilt_apk" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
printf "\t[*] Rebuilt APK: %s\n" "$(basename "$rebuilt_apk")"

# --- Remove Decompiled APK Directory ---
printf "[*] Removing decompiled APK directory '%s/apk'...\n" "$temp_dir"
rm -rf "$temp_dir/apk"
check_error "Failed to remove decompiled directory '$temp_dir/apk'."

# --- Sign Rebuilt APK ---
signed_apk="$temp_dir/signed.apk"
printf "[*] Signing rebuilt APK '%s'...\n" "$(basename "$rebuilt_apk")"
# Check if keystore exists
if [ ! -f "$keystore_path" ]; then
    printf "\t❌ Error: Keystore file not found at '%s'. Generate it first.\n" "$keystore_path" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
apksigner sign \
  --ks "$keystore_path" \
  --ks-key-alias androiddebugkey \
  --ks-pass pass:android \
  --key-pass pass:android \
  --out "$signed_apk" \
  "$rebuilt_apk"
check_error "apksigner failed to sign the APK."

# --- Verify Signed APK ---
if [ ! -s "$signed_apk" ]; then
    printf "\t❌ Error: apksigner ran but output file '%s' was not created or is empty.\n" "$signed_apk" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
printf "\t[*] Signed APK created: %s\n" "$(basename "$signed_apk")"

# --- Cleanup Intermediate Files ---
printf "[*] Cleaning up intermediate APK files...\n"
rm -f "$rebuilt_apk" "$signed_apk.idsig" # Remove unsigned APK and signature info file
check_error "Failed to remove intermediate files '$rebuilt_apk' or '$signed_apk.idsig'."

# --- Remove Original Input XAPK ---
printf "[*] Removing original input XAPK '%s'...\n" "$input_xapk"
rm -f "$input_xapk"
check_error "Failed to remove original input XAPK '$input_xapk'."

# --- Create Final Modified XAPK ---
printf "[*] Creating final modified XAPK '%s'...\n" "$output_xapk"
# Navigate into temp dir to zip contents correctly
current_dir=$(pwd)
cd "$temp_dir"
check_error "Failed to change directory to '$temp_dir'."

# Zip contents of temp dir (which should now be the signed APK + other XAPK files)
zip -q -r "$current_dir/$output_xapk" . # Zip everything in current dir (.)
check_error "Failed to create final zip file '$output_xapk'."

# Navigate back
cd "$current_dir"
check_error "Failed to change directory back to '$current_dir'."

# --- Verify Final XAPK ---
if [ ! -s "$output_xapk" ]; then
    printf "\t❌ Error: Final zip command ran but output file '%s' was not created or is empty.\n" "$output_xapk" >&2
    rm -rf "$temp_dir" # Clean up temp dir
    exit 1
fi
printf "\t[*] Final modified XAPK created: %s\n" "$output_xapk"

# --- Final Cleanup ---
printf "[*] Removing temporary directory '%s'...\n" "$temp_dir"
rm -rf "$temp_dir"
check_error "Failed to remove temporary directory '$temp_dir'."

printf "[*] XAPK modification process completed successfully!\n"
exit 0 # Explicitly exit with success code