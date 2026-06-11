from pathlib import Path
import json

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "scrape" / "data"
DATA_FILE = DATA_PATH / "data.json"
OUTPUT_FILE = DATA_PATH / "correct_data.json"

def doit():
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    cases = data["cases"]

    for case in cases:
        items = case["ITEMS"]
        for item in items:
            item_type = item["TYPE"]
            if item_type == "Knife" or item_type == "Gloves":
                item["RARITY"] = "Special"

    save_data(data)

def save_data(data):
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)

if __name__ == "__main__":
    doit()