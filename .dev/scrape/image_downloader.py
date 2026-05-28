import time
import requests
import re
import json
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent

IMAGE_DIR = SCRIPT_DIR / "raw_data" / "IMAGE"
IMAGE_DIR.mkdir(exist_ok=True)

DATA_PATH = SCRIPT_DIR / "data"

session = requests.Session()

HEADERS = {
    "User-Agent": "Personal-Non-Commercial"
}

DELAY_SECONDS = 5

def safe_filename(url):
    name = filename_from_url(url)
    return re.sub(r'[^a-zA-Z0-9_-]', '_', name)

def filename_from_url(url):
    return url.rsplit("/", 1)[1] + ".png"

def download_image(dirname, url, prc):
    filename = safe_filename(url)

    subdir = IMAGE_DIR / dirname
    subdir.mkdir(exist_ok=True)

    filepath = subdir / filename

    if filepath.exists():
        print(f"Skipping existing: {url}")
        return

    print(f"[{prc:.2f}%] Downloading: {url}")

    response = session.get(
        url,
        headers=HEADERS,
        timeout=30,
        stream=True
    )

    response.raise_for_status()

    with open(filepath, "wb") as f:
        for chunk in response.iter_content(chunk_size=8192):
            f.write(chunk)

    time.sleep(DELAY_SECONDS)

def request_locs(dirname, locs_list):
    locs_count = len(locs_list)
    for i, loc in enumerate(locs_list):
        prc = 100*(i/locs_count)
        try:
            download_image(dirname, loc, prc)
        except Exception as e:
            print(f"Failed: {loc} with {e}")
    print("\n\n-- Downloading done. --\n")

def scrape(scrape_file):
    path = DATA_PATH / "pre_processed_item_data.json"
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
        
    locs_list = []
    items = data["items"]
    for item in items:
        loc = item["image"]
        locs_list.append(loc)

    request_locs(scrape_file, locs_list)

if __name__ == "__main__":
    scrape("item")