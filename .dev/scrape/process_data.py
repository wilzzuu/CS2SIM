from pathlib import Path
import json

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
CASE_FILE = DATA_PATH / "pre_processed_case_data_backup.json"
ITEM_FILE = DATA_PATH / "pre_processed_item_data_backup.json"
OUTPUT_FILE = DATA_PATH / "new_processed_data.json"
LOOKUP_FILE = DATA_PATH / "lookup.json"
MISSING_FILE = DATA_PATH / "missing.json"

def get_case_data():
    with open(CASE_FILE, "r", encoding="utf-8") as f:
        case_data = json.load(f)
    return case_data

def get_item_data():
    with open(ITEM_FILE, "r", encoding="utf-8") as f:
        item_data = json.load(f)
    return item_data

def process(case_data, item_data):
    item_lookup = {}
    duplicates = {}

    for item in item_data["items"]:

        key = (
            item["name"].strip(),
            item["skin"].strip()
        )

        item_lookup[key] = item
        duplicates[key] = duplicates.get(key, 0) + 1

    for key, count in duplicates.items():
        if count > 1:
            print("DUPLICATE:",key, count)

    print(f"Loaded {len(item_lookup)} items")

    missing_items = []

    for case in case_data["cases"]:

        merged_items = []

        for case_item in case["items"]:

            key = (
                case_item["name"].strip(),
                case_item["skin"].strip()
            )

            full_item = item_lookup.get(key)

            if full_item is None:

                missing_items.append({
                    "case": case["name"],
                    "name": case_item["name"],
                    "skin": case_item["skin"]
                })

                continue

            merged_items.append(full_item)

        case["items"] = merged_items

    print(f"\nMissing items: {len(missing_items)}")
    save_data(case_data)
    save_missing({"missing": missing_items})

def save_data(data):
    OUTPUT_FILE.touch(exist_ok=True)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)

def save_missing(data):
    MISSING_FILE.touch(exist_ok=True)
    with open(MISSING_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)
    
def save_lookup(data):
    LOOKUP_FILE.touch(exist_ok=True)
    with open(LOOKUP_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)

if __name__ == "__main__":
    case_data = get_case_data()
    item_data = get_item_data()
    process(case_data, item_data)