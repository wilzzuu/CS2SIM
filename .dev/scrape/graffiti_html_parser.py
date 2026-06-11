
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
    return [f for f in path.iterdir() if f.is_file() and not "box" in f.name]

def loop_files():
    base_path = HTML_PATH / "graffiti"
    files = get_dir_files(base_path)

    final_graffiti_data = {"graffitis":[]}

    file_count = len(files)
    for i, f in enumerate(files):
        prc = 100*(i/file_count)
        print(f"[{prc:.2f}%] Processing file: {f}")
        html = read_html_file(f)
        graffiti_data = parse_html(html)

        graffitis = final_graffiti_data["graffitis"]
        graffitis.append(graffiti_data)
        final_graffiti_data["graffitis"] = graffitis
    save_data(final_graffiti_data)


def parse_html(html):
    soup = BeautifulSoup(html, 'lxml')

    graffiti_image_container = soup.select_one("div.skin-image-container")
    graffiti_image = graffiti_image_container.select_one("img")
    graffiti_image_src = graffiti_image["src"]
    parsed = urlparse(graffiti_image_src)
    query = parse_qs(parsed.query)
    encoded_url = query["url"][0]
    real_url = unquote(encoded_url)

    title_container = soup.select_one("h1#skin-page-main-title")
    a_list = title_container.find_all("a")
    if a_list:
        graffiti_name = a_list[0].text.strip()
        #graffiti_type = a_list[1].get_text()
    else:
        graffiti_title = title_container.text.split("|", 1)[1]
        graffiti_name = graffiti_title.split("|", 1)[0].strip()

    graffiti_rarity = soup.select_one(".quality-label").text.strip()

    graffiti_price_cards = [c for c in soup.select(".prices-card") if not c.find_parent("script")]
    graffiti_prices_container = graffiti_price_cards[0]
    graffiti_price_rows = graffiti_prices_container.select(".price-row.graffiti-row")

    graffiti_types = []
    

    for row in graffiti_price_rows:
        graffiti_style_span = row.select_one("span.price-ext-label")
        graffiti_style = graffiti_style_span.text
        graffiti_price_span = row.select_one("span.price-amount")
        graffiti_price = graffiti_price_span.text
        if not graffiti_price:
            continue
        
        if "," in graffiti_price : graffiti_price.strip(",")
        if "$" in graffiti_price : graffiti_price.strip("$")
        if "-" in graffiti_price : graffiti_price = 0

        #if "Base" in graffiti_type : entry["rarity"] = "Base Grade"
        #elif "High" in graffiti_type : entry["rarity"] = "High Grade"
        #elif "Remarkable" in graffiti_type : entry["rarity"] = "Remarkable"
        #elif "Exotic" in graffiti_type : entry["rarity"] = "Exotic"
        #else : entry["rarity"] = "None"
        
        try:
            graffiti_price = float(graffiti_price)
        except:
            graffiti_price = str(graffiti_price)

        graffiti_types.append({"style" : graffiti_style, "price": graffiti_price})
    
    graffiti_data = {
        "name" : graffiti_name,
        "rarity" : graffiti_rarity,
        "styles" : graffiti_types,
        "image" : real_url
    }

    return graffiti_data

def save_data(data):
    OUTPUT_PATH = SCRAPE_DIR / "data" / "pre_processed_graffiti_data.json"
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    print("\n\n-- Done processing. --\n")

if __name__ == "__main__":
    loop_files()