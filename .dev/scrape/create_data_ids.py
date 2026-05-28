from pathlib import Path
import json
import re

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
DATA_FILE = DATA_PATH / "processed_data.json"
OUTPUT_FILE = DATA_PATH / "data.json"

def doit():
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    cases = data["cases"]

    for case in cases:
        items = case["ITEMS"]
        for item in items:
            conds = item["CONDITIONS"]
            for cond in conds:
                if cond["stattrak_price"] != None:
                    item["HAS_ST"] = True

    save_data(data)

def save_data(data):
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)

def slugify(text):
    text = text.lower()
    text = text.replace("™", "")
    text = re.sub(r"[^a-z0-9]+", "-", text)
    text = re.sub(r"-+", "-", text)
    return text.strip("-")

if __name__ == "__main__":
    doit()