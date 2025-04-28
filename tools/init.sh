#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
# set -e
# Treat unset variables as an error when substituting.
# set -u
# Causes pipelines to fail on the first command that fails, not the last.
# set -o pipefail
# Note: Using 'set -e' can sometimes make debugging harder as the script exits
# immediately. We will use manual checks with '$?' for more control and
# specific error messages in this version.

# --- Function for Error Handling ---
# Usage: check_error "Error message if the previous command failed"
check_error() {
  local exit_code=$? # Capture the exit code of the previous command
  if [ $exit_code -ne 0 ]; then
    # Print the error message passed as an argument to standard error
    printf "\n\t❌ Error: %s (Exit Code: %d)\n" "$1" $exit_code >&2
    exit $exit_code # Exit the script with the same error code
  fi
}

# --- Argument Validation ---
if [[ $# != 0 && "$1" != "download" && "$1" != "extract" ]]; then
    printf "\tInvalid argument: %s\n" "$1" >&2
    printf "\tValid invocations are:\n" >&2
    printf "\t\t./init.sh       (Download and Extract)\n" >&2
    printf "\t\t./init.sh download (Download only)\n" >&2
    printf "\t\t./init.sh extract  (Extract only)\n" >&2
    exit 1 # Use non-zero exit for invalid arguments
fi

# --- Check for Existing XAPK (Download Step) ---
# Use a specific filename for clarity if possible
target_xapk="unmodified.xapk"

if [[ $# == 0 || "$1" == "download" ]]; then
    # Check if *any* .xapk exists first using glob
    shopt -s nullglob
    xapk_files_check=(*.xapk)
    shopt -u nullglob
    if [[ ${#xapk_files_check[@]} -gt 0 ]]; then
        printf "\t⚠️ Warning: An XAPK already exists in this directory (%s).\n" "${xapk_files_check[0]}" >&2
        printf "\tTo download a fresh version, remove all XAPKs first.\n" >&2
        # If only 'download' was specified, exit here. If default (no args), continue to extract check.
        if [[ "$1" == "download" ]]; then
             exit 1
        fi
    else
        # --- Download Step ---
        printf "\t[*] Starting download...\n"
        # Use the specific python script (assuming it's get_cf_page.py or similar)
        # Make sure download.py exists and is executable
        if [ ! -f "tools/download.py" ]; then
             printf "\t❌ Error: Download script 'download.py' not found.\n" >&2
             exit 1
        fi
        # Assuming download.py is the verbose cloudscraper script we created
        python3 tools/download.py https://d.apkpure.com/b/XAPK/com.mindcandy.warriors?version=latest "$target_xapk"
        check_error "Failed to download XAPK using download.py."

        # Verify download actually created the file
        if [ ! -f "$target_xapk" ]; then
             printf "\t❌ Error: Download script finished but output file '%s' not found.\n" "$target_xapk" >&2
             exit 1
        fi
        printf "\t[*] Download complete: %s\n" "$target_xapk"
    fi
fi

# --- Check for Existing Extraction (Extract Step) ---
if [[ $# == 0 || "$1" == "extract" ]]; then
    if [[ -d "unmodified" ]]; then
        printf "\t⚠️ Warning: 'unmodified' directory already exists.\n" >&2
        printf "\tTo perform a fresh extraction, remove the 'unmodified' directory first.\n" >&2
        exit 1 # Exit if directory exists and we intended to extract
    fi

    # --- Find the XAPK to Extract ---
    if [ ! -f "$target_xapk" ]; then
        # If unmodified.xapk doesn't exist (e.g., user ran ./init.sh extract with a different xapk name)
        shopt -s nullglob
        xapk_files_extract=(*.xapk)
        shopt -u nullglob
        if [ ${#xapk_files_extract[@]} -ne 1 ]; then
             printf "\t❌ Error: Expected exactly one .xapk file for extraction, found %d. Ensure '%s' or one other .xapk exists.\n" "${#xapk_files_extract[@]}" "$target_xapk" >&2
             exit 1
        fi
        xapk_to_extract="${xapk_files_extract[0]}"
        printf "\t[*] Using existing XAPK: %s\n" "$xapk_to_extract"
    else
         xapk_to_extract="$target_xapk"
    fi

    # --- Extraction Steps ---
    printf "\t[*] Creating output directories...\n"
    mkdir unmodified
    check_error "Failed to create directory 'unmodified'."
    mkdir unmodified/bin
    check_error "Failed to create directory 'unmodified/bin'."

    printf "\t[*] Unzipping XAPK: %s...\n" "$xapk_to_extract"
    unzip -q "$xapk_to_extract" # Use -q for quieter unzip unless debugging
    check_error "Failed to unzip '$xapk_to_extract'. Check 'unzip' command and file integrity."

    printf "\t[*] Removing temporary XAPK contents...\n"
    rm -rf Android manifest.json icon.png # Use -f to ignore non-existent files
    check_error "Failed during initial cleanup after unzipping XAPK (permissions?)."

    # Find the main APK file (usually only one)
    shopt -s nullglob
    apk_files=(*.apk)
    shopt -u nullglob
    if [ ${#apk_files[@]} -ne 1 ]; then
        printf "\t❌ Error: Expected exactly one .apk file after unzipping XAPK, found %d.\n" "${#apk_files[@]}" >&2
        exit 1
    fi
    apk_file="${apk_files[0]}"

    printf "\t[*] Unzipping APK: %s...\n" "$apk_file"
    unzip -q "$apk_file" # Use -q for quieter unzip
    check_error "Failed to unzip '$apk_file'. Check 'unzip' command and file integrity."

    printf "\t[*] Moving managed DLLs...\n"
    # Check if source directory exists
    if [ ! -d "assets/bin/Data/Managed" ]; then
         printf "\t❌ Error: Source directory 'assets/bin/Data/Managed' not found after unzipping APK.\n" >&2
         exit 1
    fi
    # Check if there are files to move
    shopt -s nullglob
    dll_files_to_move=(assets/bin/Data/Managed/*)
    shopt -u nullglob
    if [ ${#dll_files_to_move[@]} -eq 0 ]; then
        printf "\t⚠️ Warning: No files found in 'assets/bin/Data/Managed/' to move.\n" >&2
    else
        mv assets/bin/Data/Managed/* unmodified/bin/
        check_error "Failed to move files from 'assets/bin/Data/Managed' to 'unmodified/bin'."
    fi

    printf "\t[*] Removing temporary APK contents...\n"
    # Add the extracted apk file itself to the cleanup list
    rm -rf AndroidManifest.xml assets com.mindcandy.warriors.apk res lib resources.arsc META-INF classes.dex "$apk_file"
    check_error "Failed during cleanup after unzipping APK (permissions?)."

    printf "\t[*] Creating decompile/disassembly directories...\n"
    mkdir unmodified/decompiled
    check_error "Failed to create directory 'unmodified/decompiled'."
    mkdir unmodified/il
    check_error "Failed to create directory 'unmodified/il'."

    printf "\t[*] Processing DLLs...\n"
    processed_dll_count=0
    shopt -s nullglob # Enable nullglob for the loop
    for dll in unmodified/bin/*.dll; do
        filename=$(basename "$dll" .dll)
        printf "\n\t--- Processing: %s ---\n" "$filename.dll"

        # Decompile
        printf "\t\tDecompiling with ilspycmd...\n"
        ilspycmd -o "unmodified/decompiled" "$dll"
        if [ $? -ne 0 ]; then
            printf "\t\t⚠️ Warning: ilspycmd failed for '%s'. Skipping decompilation.\n" "$dll" >&2
        fi

        # Disassemble (Using the command specified by the user: ikdasm)
        printf "\t\tDisassembling with ikdasm...\n"
        # Ensure output directory exists before redirecting output
        if [ ! -d "unmodified/il" ]; then
             printf "\t\t❌ Error: Output directory 'unmodified/il' missing for ikdasm.\n" >&2
             continue # Skip this DLL
        fi
        # --- Using ikdasm as requested ---
        ikdasm "$dll" > "unmodified/il/$filename.il"
        if [ $? -ne 0 ]; then
            printf "\t\t⚠️ Warning: ikdasm failed for '%s'. Skipping disassembly.\n" "$dll" >&2
        else
             # Verify output file was created
             if [ ! -f "unmodified/il/$filename.il" ]; then
                  printf "\t\t⚠️ Warning: ikdasm ran but output file 'unmodified/il/%s.il' was not created.\n" "$filename" >&2
             fi
        fi
        processed_dll_count=$((processed_dll_count + 1))
    done
    shopt -u nullglob # Disable nullglob

    if [ $processed_dll_count -eq 0 ]; then
         printf "\t⚠️ Warning: No DLL files were found or processed in 'unmodified/bin/'.\n" >&2
    fi

    printf "\n\t[*] Extraction and processing complete.\n"
fi

exit 0 # Explicitly exit with success code