from pathlib import Path
import json
import re

SCRAPE_DIR = Path(__file__).resolve().parent
DATA_PATH = SCRAPE_DIR / "data"
INPUT_FILE = DATA_PATH / "id_changed_data.json"
OUTPUT_FILE = DATA_PATH / "case_indexes.txt"

def get_data():
    with open(INPUT_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
    return data


def doit(data):
    data_str = ""
    for i, case in enumerate(data["cases"]):
        case_idx = i
        case_name = case["NAME"]
        data_str += f"{case_idx} - {case_name}\n"
    
    save_data(data_str)

def save_data(data):
    OUTPUT_FILE.touch(exist_ok=True)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write(data)

if __name__ == "__main__":
    data = get_data()
    doit(data)