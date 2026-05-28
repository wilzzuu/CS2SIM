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
    return [f for f in path.iterdir() if f.is_file()]

def loop_files():
    base_path = HTML_PATH / "item"
    files = get_dir_files(base_path)

    final_item_data = {"items":[]}

    file_count = len(files)
    for i, f in enumerate(files):
        prc = 100*(i/file_count)
        print(f"[{prc:.2f}%] Processing file: {f}")
        html = read_html_file(f)
        item_data = parse_html(html)

        items = final_item_data["items"]
        items.append(item_data)
        final_item_data["items"] = items
    save_data(final_item_data)


def parse_html(html):
    soup = BeautifulSoup(html, 'lxml')

    item_image_container = soup.select_one("div.skin-image-container")
    item_image = item_image_container.select_one("img")
    item_image_src = item_image["src"]
    parsed = urlparse(item_image_src)
    query = parse_qs(parsed.query)
    encoded_url = query["url"][0]
    real_url = unquote(encoded_url)

    item_title_container = soup.select_one("h1#skin-page-main-title")
    a_list = item_title_container.find_all("a")
    item_name = a_list[0].text
    item_skin = a_list[1].text

    item_quality_type = soup.find(class_="quality-label").text
    item_quality_type = item_quality_type.strip("★ ")
    if "Grade" in item_quality_type:
        item_quality_type= item_quality_type.replace("Grade ", "")
    item_quality, item_type = item_quality_type.split(" ", 1)

    try:
        item_collection = soup.find(class_="collection-name").text
    except Exception:
        item_collection = "None"
    try: 
        item_container = soup.find(class_="case-name").text
    except Exception:
        item_container = "None"

    item_price_cards = [c for c in soup.select(".prices-card") if not c.find_parent("script")]
    item_prices_container = item_price_cards[0]
    if item_skin != "Vanilla":
        item_price_rows = item_prices_container.select(".price-table-row")

        item_conditions = {"conditions": []}
        item_conds = item_conditions["conditions"]

        for row in item_price_rows:
            item_condition = row["data-wear-exterior"]

            price_cells = row.select(".price-table-cell")

            entry = {
                "condition" : item_condition,
                "regular_price" : None,
                "stattrak_price": None
            }
            
            for cell in price_cells:
                
                price = cell.get_text(strip=True)
                if not price:
                    continue
                
                if "st" in cell.get("class", []):
                    entry["stattrak_price"] = price
                else:
                    entry["regular_price"] = price
            
            templates = row.find_all("template")
            if templates:
                #print(templates)
                if len(templates) == 1:
                    template_id = templates[0].get("id")
                    hidden_id = template_id.replace("P:", "S:")
                    hidden = soup.find("div", id=hidden_id)
                    price_cells = hidden.select(".price-table-cell")
                    if price_cells:
                        for cell in price_cells:
                            price = cell.get_text(strip=True)
                            if not price:
                                continue

                            if not "st" in cell.get("class", []) and entry["regular_price"] == None or entry["regular_price"] == "" and entry["regular_price"] != price:
                                entry["regular_price"] = price
                            elif "st" in cell.get("class", []) and entry["stattrak_price"] == None or entry["stattrak_price"] == "" and entry["stattrak_price"] != price:
                                entry["stattrak_price"] = price
                    else:
                        price = hidden.get_text(strip=True)
                        if entry["regular_price"] == None or entry["regular_price"] == "" and entry["regular_price"] != price:
                            entry["regular_price"] = price
                        elif entry["stattrak_price"] == None or entry["stattrak_price"] == "" and entry["stattrak_price"] != price:
                            entry["stattrak_price"] = price
                    
                elif len(templates) > 1:
                    temp = templates[0]
                    temp_id = temp.get("id")
                    hidden_id = temp_id.replace("P:", "S:")
                    hidden = soup.find("div", id=hidden_id)
                    price = hidden.get_text(strip=True)
                    if entry["regular_price"] == None or entry["regular_price"] == "" and entry["regular_price"] != price:
                        entry["regular_price"] = price

                    st_temp = templates[1]
                    st_temp_id = st_temp.get("id")
                    st_hidden_id = st_temp_id.replace("P:", "S:")
                    st_hidden = soup.find("div", id=st_hidden_id)
                    st_price = st_hidden.get_text(strip=True)
                    if entry["stattrak_price"] == None or entry["stattrak_price"] == "" and entry["stattrak_price"] != price:
                        entry["stattrak_price"] = st_price

            item_conds.append(entry)
    else:
        item_conditions = {"conditions": []}
        item_conds = item_conditions["conditions"]

        entry = {
            "condition" : "Vanilla",
            "regular_price" : None,
            "stattrak_price": None
        }
        
        item_price_span = item_prices_container.select_one(".price-amount")
        item_price = item_price_span.get_text(strip=True)
        entry["regular_price"] = item_price
        item_conds.append(entry)

    item_data = {
        "name" : item_name,
        "skin" : item_skin,
        "quality" : item_quality,
        "type" : item_type,
        "conditions" : item_conds,
        "collection" : item_collection,
        "container" : item_container,
        "image" : real_url
    }
    return item_data

def save_data(data):
    OUTPUT_PATH = SCRAPE_DIR / "data" / "pre_processed_item_data.json"
    with open(OUTPUT_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)
    print("\n\n-- Done processing. --\n")

if __name__ == "__main__":
    loop_files()