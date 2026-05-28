from bs4 import BeautifulSoup
from pathlib import Path
import json

scrape_dir = Path(__file__).resolve().parent
html_dir = scrape_dir / "HTML"
raw_data_dir = scrape_dir / "raw_data"

main_page = html_dir / "CS2 Skins & CSGO Skins - Browse, Buy, Inspect Skins (CS_GO Stash).html"
containers_page = html_dir / "CS2 Cases - All CS2 & CS_GO Skin Case Prices.html"
container_items_page = html_dir / "Sealed Dead Hand Terminal Skins (CS2 & CS_GO) - Sealed Dead Hand Terminal Price & Stats.html"
item_page = html_dir / "AWP _ Queen's Gambit - CS2 Prices (557 Listings).html"
container_specials_page = html_dir / "Sealed Dead Hand Terminal Gloves - CS2 Skins.html"
special_page = html_dir / "Driver Gloves Garden - CS2 Price, Float, Inspect (CS_GO).html"


####################################################################################################
"""

with open(containers_page, "r", encoding="utf-8") as file:
    containers_html = file.read()
containers_soup = BeautifulSoup(containers_html, "html.parser")

main_content = containers_soup.find(class_="container main-content")
main_content_items = main_content.find_all(class_="col-lg-4 col-md-6 col-widen text-center")

containers_data = {"cases":[]}
cases_list = []

for mcitem in main_content_items:
    item_core = mcitem.div
    child_url = item_core.a.get("href")
    container_name = item_core.a.h4.text
    container_price = item_core.a.div.p.text
    container_price = container_price.strip("€")
    cases_list.append({"name": container_name, "price": container_price, "link": child_url, "items": []})
    containers_data["cases"] = cases_list
    #containers_data[container_name] = {"price": container_price, "link": child_url, "items" : []}

with open(raw_data_dir / "raw_containers.json", "w", encoding="utf-8") as file:
    json.dump(containers_data, file, indent=4)

"""
####################################################################################################
"""

with open(raw_data_dir / "raw_containers.json", "r", encoding="utf-8") as file:
    data = json.load(file)
cases = data["cases"]


with open(container_items_page, "r", encoding="utf-8") as file:
    container_items_html = file.read()
container_items_soup = BeautifulSoup(container_items_html, "html.parser")

main_content = container_items_soup.find(class_="case-page-main-row row")
main_content_items = main_content.find_all(class_="col-lg-4 col-md-6 col-widen text-center")

container_items_list = []

for mcitem in main_content_items[1:]:
    item_core = mcitem.div
    item_a = item_core.h2.contents
    item_weapon_name = item_a[0].text
    item_skin_name = item_a[2].text
    item_type_data = item_core.find(class_="nounderline")
    item_type_data_parts = item_type_data.div.p.text.strip().split(" ", 1)
    item_rarity = item_type_data_parts[0]
    item_type = item_type_data_parts[1]
    grandchild_url = item_core.find_all("a")[3].get("href")
    container_items_list.append({
        "weapon": item_weapon_name,
        "name": item_skin_name,
        "rarity": item_rarity,
        "type": item_type,
        "conditions": []
        })

cases[0]["items"] = container_items_list

with open(raw_data_dir / "raw_containers.json", "w", encoding="utf-8") as file:
    json.dump(data, file, indent=4)

"""
####################################################################################################


with open(raw_data_dir / "raw_containers.json", "r", encoding="utf-8") as file:
    data = json.load(file)
cases = data["cases"]

with open(item_page, "r", encoding="utf-8") as file:
    item_html = file.read()
item_soup = BeautifulSoup(item_html, "html.parser")

main_content = item_soup.body.find(class_="container main-content")
main_content_sub = main_content.find_all(class_="row text-center")[1]
table_body = main_content_sub.find("tbody")
table_items = table_body.find_all("tr")

item_conditions_list = []

for tr in table_items:
    tr_data = tr.contents
    item_condition = tr_data[1].text.strip()
    item_price = tr_data[3].a.text.strip("€").strip()
    item_conditions_list.append({"condition": item_condition, "price": item_price})

cases[0]["items"][0]["conditions"] = item_conditions_list

with open(raw_data_dir / "raw_containers.json", "w", encoding="utf-8") as file:
    json.dump(data, file, indent=4)

####################################################################################################
"""

with open(container_specials_page, "r", encoding="utf-8") as file:
    container_specials_html = file.read()
container_specials_soup = BeautifulSoup(container_specials_html, "html.parser")

"""
####################################################################################################
"""

with open(special_page, "r", encoding="utf-8") as file:
    special_html = file.read()
special_soup = BeautifulSoup(special_html, "html.parser")

"""