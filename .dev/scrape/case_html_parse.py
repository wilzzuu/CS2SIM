from bs4 import BeautifulSoup
from pathlib import Path
from urllib.parse import urlparse, parse_qs, unquote
import json

SCRAPE_DIR = Path(__file__).resolve().parent
RAW_DATA_PATH = SCRAPE_DIR / "raw_data"
HTML_PATH = RAW_DATA_PATH / "HTML"

def read_html_file(path):
    with open(path, "r", encoding="utf-8") as f:
        html = f.read()
    return html

def get_dir_files(path):
    return [f for f in path.iterdir() if f.is_file()]

def loop_files():
    base_path = HTML_PATH / "case"
    files = get_dir_files(base_path)

    case_data = {"cases":[]}

    file_count = len(files)
    for i, f in enumerate(files):
        prc = 100*(i/file_count)
        print(f"[{prc:.2f}%] Processing file: {f}")
        html = read_html_file(f)
        case_name, case_price, real_url, case_items = parse_html(html)
        cases = case_data["cases"]
        cases.append({"name": case_name, "price": case_price, "items": case_items, "image": real_url})
        case_data["cases"] = cases
    save_data(case_data)


def parse_html(html):
    soup = BeautifulSoup(html, 'html.parser')

    image_container = soup.find(class_="hero-compact")
    item_image = image_container.select_one("img")
    item_image_src = item_image["src"]
    parsed = urlparse(item_image_src)
    query = parse_qs(parsed.query)
    encoded_url = query["url"][0]
    real_url = unquote(encoded_url)

    name_container = soup.find(class_="container mb-1")
    case_name = name_container.find(class_="active").text

    spans = soup.find_all(class_="hero-compact-stat-value")
    case_price = spans[3].text

    case_items = []

    grid_items = soup.select(".grid-item-modern")
    for grid in grid_items:
        item_card_model = grid.select_one(".item-card-model")
        try:
            item_name = item_card_model.get_text(strip=True)
        except Exception:
            item_name = ""
        item_card_name_row = grid.select_one(".item-card-name-row")
        try:
            item_skin = item_card_name_row.get_text(strip=True)
        except Exception:
            item_skin = ""
        if "Broken Fang Gloves" in item_skin:
            item_name = "Broken Fang Gloves"
            item_skin = item_skin.split("Broken Fang Gloves", 1)[1]
            
        filters = ["Doppler", "Gamma Doppler", "Emerald", "Needle Point", "Yellow-banded", "Jade", "Unhinged", "Olympus", "Tosai"]
        if item_skin not in filters:
            case_items.append({"name": item_name, "skin": item_skin})

    return case_name, case_price, real_url, case_items

def save_data(data):
    OUTPUT_PATH = SCRAPE_DIR / "data" / "pre_processed_case_data.json"
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)

if __name__ == "__main__":
    loop_files()