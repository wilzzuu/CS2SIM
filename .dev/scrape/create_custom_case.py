from pathlib import Path
import json
import re

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
INPUT_FILE = DATA_PATH / "id_changed_data.json"

def get_data():
    with open(INPUT_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
    return data

def doit(data):
    
    special_items = []

    case = data["cases"][13]
    items = case["ITEMS"]
    item_count = 0
    total_price = 0
    for item in items:
        if item["RARITY"] == "Special":
            for cond in item["CONDITIONS"]:
                item_count += 1
                total_price += cond["regular_price"]
                if cond["stattrak_price"] != 0 or cond["stattrak_price"] != None:
                    total_price += cond["stattrak_price"]
                    item_count += 1
            special_items.append(item)
    
    special_price = total_price / item_count
    special_price *= 1.50

    specials = {"cases" : [{"ID": slugify(custom_case_name), "NAME": custom_case_name, "PRICE": round(special_price, 2), "ITEMS": special_items}]}

    save_data(specials)

def slugify(text):
    text = text.lower()
    text = text.replace("™", "")
    text = re.sub(r"[^a-z0-9]+", "-", text)
    text = re.sub(r"-+", "-", text)
    return text

def save_data(data):
    OUTPUT_FILE.touch(exist_ok=True)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)

if __name__ == "__main__":
    custom_case_name = input("Custom Case Name:\n")
    OUTPUT_FILE = DATA_PATH / f"{slugify(custom_case_name)}.json"
    data = get_data()
    doit(data)