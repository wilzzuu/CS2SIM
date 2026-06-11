
from bs4 import BeautifulSoup
from pathlib import Path
from urllib.parse import urlparse, parse_qs, unquote
import json
import re

SCRAPE_DIR = Path(__file__).resolve().parent
RAW_DATA_PATH = SCRAPE_DIR / "raw_data"
HTML_PATH = RAW_DATA_PATH / "HTML"

def read_html_file(path):
    with open(path, "r", encoding="utf-8") as f:
        html = f.read()
    return html

def get_dir_files(path):
    return [f for f in path.iterdir() if f.is_file() and not "capsule" in f.name]

def loop_files():
    base_path = HTML_PATH / "sticker"
    files = get_dir_files(base_path)

    final_sticker_data = {"stickers":[]}

    file_count = len(files)
    for i, f in enumerate(files):
        prc = 100*(i/file_count)
        print(f"[{prc:.2f}%] Processing file: {f}")
        html = read_html_file(f)
        sticker_data = parse_html(html)

        stickers = final_sticker_data["stickers"]
        stickers.append(sticker_data)
        final_sticker_data["stickers"] = stickers
    save_data(final_sticker_data)


def parse_html(html):
    soup = BeautifulSoup(html, 'lxml')

    sticker_image_container = soup.select_one("div.skin-image-container")
    sticker_image = sticker_image_container.select_one("img")
    sticker_image_src = sticker_image["src"]
    parsed = urlparse(sticker_image_src)
    query = parse_qs(parsed.query)
    encoded_url = query["url"][0]
    real_url = unquote(encoded_url)

    try:
        sticker_collection = soup.find("collection-name").text
    except:
        sticker_collection = "None"

    title_container = soup.select_one("h1#skin-page-main-title")
    a_list = title_container.find_all("a")
    if a_list:
        sticker_name = a_list[0].get_text(strip=True)
        sticker_style = a_list[1].get_text()
    else:
        sticker_title = title_container.text.split("|", 1)[1]
        sticker_name = sticker_title.split("|", 1)
    
    sticker_price_cards = [c for c in soup.select(".prices-card") if not c.find_parent("script")]
    sticker_prices_container = sticker_price_cards[0]
    sticker_price_rows = sticker_prices_container.select(".price-row")

    sticker_styles = []

    for row in sticker_price_rows:
        sticker_style = row.select_one(".price-ext-label")
        sticker_price = row.select_one(".price-amount")
        if not sticker_price:
            continue
        
        if "," in sticker_price : sticker_price.strip(",")
        if "$" in sticker_price : sticker_price.strip("$")

        if sticker_style == "Regular" : sticker_rarity = "High Grade"
        elif sticker_style == "Glossy" : sticker_rarity = "High Grade"
        elif sticker_style == "Glitter" : sticker_rarity = "Remarkable"
        elif sticker_style == "Holo" : sticker_rarity = "Exotic"
        elif sticker_style == "Foil" : sticker_rarity = "Exotic"
        elif sticker_style == "Gold" : sticker_rarity = "Extraordinary"
        else : sticker_rarity = "None"

        try:
            sticker_price = float(sticker_price)
        except:
            sticker_price = str(sticker_price)

        sticker_styles.append({"style" : sticker_style, "price" : sticker_price})
    
    sticker_data = {
        "name" : sticker_name,
        "rarity" : sticker_rarity,
        "styles" : sticker_styles,
        "collection" : sticker_collection,
        "image" : real_url
    }

    return sticker_data

def save_data(data):
    OUTPUT_PATH = SCRAPE_DIR / "data" / "pre_processed_sticker_data.json"
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    print("\n\n-- Done processing. --\n")

if __name__ == "__main__":
    loop_files()