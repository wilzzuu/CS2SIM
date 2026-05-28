import time
import requests
import json
from pathlib import Path
from PIL import Image
from io import BytesIO

SCRIPT_DIR = Path(__file__).resolve().parent

IMAGE_DIR = SCRIPT_DIR / "data" / "IMAGE"
IMAGE_DIR.mkdir(exist_ok=True)

DATA_PATH = SCRIPT_DIR / "data"
DATA_FILE = DATA_PATH / "data.json"

session = requests.Session()

HEADERS = {
    "User-Agent": "Personal-Non-Commercial"
}

DELAY_SECONDS = 6

def download_image(filename, dirname, url, prc):
    subdir = IMAGE_DIR / dirname
    subdir.mkdir(exist_ok=True)

    filepath = subdir / f"{filename}.png"

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
    caseIDs = [id["ID"] for id in locs_list]
    locs = [loc["IMAGE"] for loc in locs_list]
    for i, loc in enumerate(locs):
        prc = 100*(i/locs_count)
        try:
            download_image(caseIDs[i], dirname, loc, prc)
        except Exception as e:
            print(f"Failed: {loc} with {e}")
    print("\n\n-- Downloading done. --\n")

def scrape(scrape_file):
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
        
    locs_list = []
    for case in data["cases"]:
        for item in case["ITEMS"]:
            itemID = item["ID"]
            loc = item["IMAGE"]
            locs_list.append({"ID": itemID, "IMAGE": loc})

    request_locs(scrape_file, locs_list)

if __name__ == "__main__":
    scrape("item")