import time
import requests
from pathlib import Path

HEADERS = {
    "User-Agent": "Personal-Non-Commercial"
}

SCRIPT_DIR = Path(__file__).resolve().parent

OUTPUT_DIR = SCRIPT_DIR / "raw_data" / "HTML"
OUTPUT_DIR.mkdir(exist_ok=True)

RAW_DATA_PATH = SCRIPT_DIR / "raw_data"

session = requests.Session()

def filename_from_url(url):
    return url.rsplit("/", 1)[1] + ".html"

def download_page(dirname, url, prc):
    filename = filename_from_url(url)

    subdir = OUTPUT_DIR / dirname
    subdir.mkdir(exist_ok=True)

    filepath = subdir / filename

    if filepath.exists():
        if filepath.stat().st_size != 0:
            print(f"Skipping: {url}")
            return   
        
    filepath.touch(exist_ok=True)
    
    print(f"[{prc:.2f}%] Downloading: {url}")

    response = session.get(
        url,
        headers=HEADERS,
        timeout=30
    )

    response.raise_for_status()

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(response.text)

    time.sleep(5)

def request_locs(dirname, locs_list):
    locs_count = len(locs_list)
    for i, loc in enumerate(locs_list):
        prc = 100*(i/locs_count)
        try:
            download_page(dirname, loc, prc)
        except Exception as e:
            print(f"Failed: {loc} with {e}")
    print("\n\n-- Downloading done. --\n")

def scrape(scrape_file):
    path = RAW_DATA_PATH / f"{scrape_file}_locs.txt"
    with open(path, "r", encoding="utf-8") as f:
        locs_list = [line.strip("\n") for line in f.readlines()]

    request_locs(scrape_file, locs_list)

if __name__ == "__main__":
    scrape("graffiti")