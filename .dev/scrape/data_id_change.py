from pathlib import Path
import json
import re

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
IMAGE_PATH = DATA_PATH / "IMAGE"
CASE_IMAGE_PATH = IMAGE_PATH / "case"
ITEM_IMAGE_PATH = IMAGE_PATH / "item"
DATA_FILE = DATA_PATH / "data.json"
OUTPUT_FILE = DATA_PATH / "id_changed_data.json"

def doit():
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    cases = data["cases"]

    for case in cases:
        case_id = case["ID"]
        case_name = case["NAME"]
        new_case_id = slugify(case_name)
        
        for case_image in CASE_IMAGE_PATH.iterdir():
            if case_image.is_file():
                if case_image.name == f"{case_id}.png":
                    print(case_image)

                    new_path = case_image.with_name(f"{new_case_id}.png")

                    if new_path.exists():
                        print(f"Duplicate found: {new_path.name}")
                        case_image.unlink()
                    else:
                        case_image.rename(new_path)

        case["ID"] = new_case_id

        items = case["ITEMS"]
        for item in items:
            item_id = item["ID"]
            item_gun = item["GUN"]
            item_name = item["NAME"]
            new_item_id = slugify(f"{item_gun}-{item_name}")

            for item_image in ITEM_IMAGE_PATH.iterdir():
                if item_image.is_file():
                    if item_image.name == f"{item_id}.png":
                        print(item_image)

                        new_path = item_image.with_name(f"{new_item_id}.png")

                        if new_path.exists():
                            print(f"Duplicate found: {new_path.name}")
                            item_image.unlink()
                        else:
                            item_image.rename(new_path)

            item["ID"] = new_item_id

    save_data(data)

def slugify(text):
    text = text.lower()
    text = text.replace("™", "")
    text = re.sub(r"[^a-z0-9]+", "-", text)
    text = re.sub(r"-+", "-", text)
    return text

def save_data(data):
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)

if __name__ == "__main__":
    doit()