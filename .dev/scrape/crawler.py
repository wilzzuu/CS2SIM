import os
import time
import requests
from pathlib import Path


DELAY_SECONDS = 5

scrape_dir = Path(__file__).resolve().parent
BASE_DIR = scrape_dir / "data" / "cases"
BASE_DIR.mkdir(exist_ok=True)

session = requests.Session()


def save_page(url, filepath):
    if filepath.exists():
        print(f"Skipping existing: {filepath}")
        return
    
    print(f"Downloading: {url}")

    response = session.get(url, timeout=30)

    response.raise_for_status()

    filepath.parent.mkdir(parents=True, exist_ok=True)

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(response.text)

    time.sleep(DELAY_SECONDS)


url = "https://stash.clash.gg/case/422/Fever-Case"
case_name = url.rsplit("/", 1)
filepath = BASE_DIR / f"{case_name}.html"

save_page(url, filepath)
