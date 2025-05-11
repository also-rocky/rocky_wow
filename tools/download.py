#!/usr/bin/env python3
import cloudscraper
import sys
import os
import time # For potential throttling if needed, not strictly used now
import math # For progress bar calculation

# --- Configuration ---
# How many bytes to read/write at a time when streaming downloads
CHUNK_SIZE = 8192
# Width of the text-based progress bar
PROGRESS_BAR_WIDTH = 50

# --- Helper Function for Progress Bar ---
def print_progress(iteration, total, prefix='', suffix='', decimals=1, length=100, fill='#'):
    """
    Call in a loop to create terminal progress bar
    @params:
        iteration   - Required  : current iteration (Int)
        total       - Required  : total iterations (Int)
        prefix      - Optional  : prefix string (Str)
        suffix      - Optional  : suffix string (Str)
        decimals    - Optional  : positive number of decimals in percent complete (Int)
        length      - Optional  : character length of bar (Int)
        fill        - Optional  : bar fill character (Str)
    """
    if total <= 0: # Avoid division by zero if total size is unknown or zero
        print(f'\r{prefix} [Progress unavailable] {suffix}', end = '', file=sys.stderr)
        return

    percent = ("{0:." + str(decimals) + "f}").format(100 * (iteration / float(total)))
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {percent}% {suffix}', end = '', file=sys.stderr)
    # Print New Line on Complete
    if iteration == total:
        print(file=sys.stderr) # Print to stderr to not interfere with stdout


# --- Argument Parsing ---
if len(sys.argv) < 2:
    # Print usage instructions to standard error
    print("Usage: python3 get_cf_page.py <URL> [output_file]", file=sys.stderr)
    sys.exit(1) # Exit with a non-zero status code indicates error

url = sys.argv[1]
# Determine the output file path if provided, otherwise set to None
output_file = sys.argv[2] if len(sys.argv) > 2 else None

# --- Initialization ---
print(f"[*] Initializing Cloudflare scraper...", file=sys.stderr)
# Create a scraper instance. You might tweak browser hints if needed.
scraper = cloudscraper.create_scraper(
    browser={'browser': 'chrome', 'platform': 'linux', 'mobile': False},
    # delay=10 # Optional: Add delay between challenge retries/requests
)

print(f"[*] Target URL: {url}", file=sys.stderr)
if output_file:
    print(f"[*] Output File: {output_file}", file=sys.stderr)
else:
    print(f"[*] Outputting content to standard output (stdout)", file=sys.stderr)

# --- Main Execution Block ---
try:
    print(f"[*] Attempting to fetch content...", file=sys.stderr)

    # Determine if we need to stream the response (for file downloads)
    should_stream = output_file is not None

    # Make the request using the scraper
    # stream=True allows downloading content in chunks (important for large files)
    # timeout prevents the script from hanging indefinitely
    response = scraper.get(url, verify=True, stream=should_stream, timeout=90) # Increased timeout

    print(f"[*] Received initial response. Status Code: {response.status_code}", file=sys.stderr)
    # Check if the final request (after potential challenges/redirects) was successful (e.g., 200 OK)
    # This will raise an HTTPError exception for 4xx or 5xx status codes.
    response.raise_for_status()
    print(f"[*] Successfully passed Cloudflare (if present) and got successful HTTP status.", file=sys.stderr)


    # --- Process the Response ---
    if output_file:
        # --- Saving to File ---
        print(f"[*] Starting download to {output_file}...", file=sys.stderr)

        # Try to get the total file size from headers for the progress bar
        total_size_in_bytes_str = response.headers.get('content-length')
        total_size = 0
        if total_size_in_bytes_str:
            try:
                total_size = int(total_size_in_bytes_str)
                print(f"[*] Total file size: {total_size / (1024*1024):.2f} MB", file=sys.stderr)
            except ValueError:
                print("[!] Warning: Could not parse 'content-length' header.", file=sys.stderr)
                total_size = 0 # Treat as unknown size
        else:
            print("[!] Warning: 'content-length' header missing. Progress percentage unavailable.", file=sys.stderr)

        downloaded_size = 0
        # Open the output file in binary write mode ('wb')
        with open(output_file, 'wb') as f:
            # Iterate over the response content in chunks
            for chunk in response.iter_content(chunk_size=CHUNK_SIZE):
                if chunk:  # filter out keep-alive new chunks
                    f.write(chunk)
                    downloaded_size += len(chunk)
                    # Update progress bar
                    size_suffix = f"{downloaded_size / (1024*1024):.2f} MB / {total_size / (1024*1024):.2f} MB" if total_size > 0 else f"{downloaded_size / (1024*1024):.2f} MB"
                    print_progress(downloaded_size, total_size, prefix='[*] Progress:', suffix=size_suffix, length=PROGRESS_BAR_WIDTH)

        # Ensure the progress bar line is cleared or finalized after loop
        if total_size == 0 or downloaded_size != total_size:
             print(file=sys.stderr) # Print a newline if progress didn't hit 100% or was unknown

        print(f"[*] Successfully downloaded and saved to {output_file}", file=sys.stderr)

    else:
        # --- Printing to Standard Output ---
        print(f"[*] Reading content to print to stdout...", file=sys.stderr)
        # Accessing .text will read the entire response body into memory.
        # This is suitable for HTML/text, but could consume lots of RAM for huge files.
        content = response.text
        print(f"[*] Content read. Printing now:", file=sys.stderr)
        # Print the actual content to standard output
        print(content)
        # Note: No final confirmation here as the content itself is the confirmation.

# --- Error Handling ---
except cloudscraper.exceptions.CloudflareChallengeError as e:
    print(f"\n[!] Cloudflare challenge failed.", file=sys.stderr)
    print(f"[!] Error details: {e}", file=sys.stderr)
    sys.exit(2) # Use a specific exit code for CF errors
except requests.exceptions.HTTPError as e:
    print(f"\n[!] HTTP Error after Cloudflare check.", file=sys.stderr)
    print(f"[!] Status Code: {e.response.status_code}", file=sys.stderr)
    print(f"[!] URL: {e.request.url}", file=sys.stderr)
    # Optionally print response body for debugging, might be large
    # print(f"[!] Response: {e.response.text[:500]}...", file=sys.stderr)
    sys.exit(3) # Specific exit code for HTTP errors
except requests.exceptions.RequestException as e:
    # Catch other potential network errors (DNS, Connection, Timeout etc.)
    print(f"\n[!] Network or Request Error.", file=sys.stderr)
    print(f"[!] Error details: {e}", file=sys.stderr)
    sys.exit(4) # Specific exit code for network errors
except Exception as e:
    # Catch any other unexpected errors
    print(f"\n[!] An unexpected error occurred.", file=sys.stderr)
    print(f"[!] Error type: {type(e).__name__}", file=sys.stderr)
    print(f"[!] Error details: {e}", file=sys.stderr)
    sys.exit(1) # General error exit code

# --- Success ---
# A final confirmation message to stderr upon successful completion
print(f"[*] Script finished successfully.", file=sys.stderr)