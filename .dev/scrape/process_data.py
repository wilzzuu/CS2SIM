from pathlib import Path
import json

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
CASE_FILE = DATA_PATH / "pre_processed_case_data_backup.json"
ITEM_FILE = DATA_PATH / "pre_processed_item_data_backup.json"
OUTPUT_FILE = DATA_PATH / "processed_data.json"

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

    for item in item_data["items"]:

        key = (
            item["name"].strip().lower(),
            item["skin"].strip().lower()
        )

        item_lookup[key] = item

    for case in case_data["cases"]:
        for case_item in case["items"]:

            key = (
                case_item["name"].strip().lower(),
                case_item["skin"].strip().lower()
            )

            matched_item = item_lookup.get(key)

            if matched_item:
                case_item.update(matched_item)
            else:
                print("MISSING:", key)
    save_data(case_data)

def save_data(data):
    OUTPUT_FILE.touch(exist_ok=True)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)

if __name__ == "__main__":
    case_data = get_case_data()
    item_data = get_item_data()
    process(case_data, item_data)