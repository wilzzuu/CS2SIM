from pathlib import Path
from bs4 import BeautifulSoup
import requests
import time
import json

scrape_dir = Path(__file__).resolve().parent
CASE_URL_FILE = scrape_dir / "raw_data" / "case_urls.txt"
BASE_DIR = scrape_dir / "data"
BASE_DIR.mkdir(exist_ok=True)

BASE_URL = "https://stash.clash.gg/"

containers_data = {"cases":[]}

def get_response(url):
    case_name = url.rsplit("/", 1)
    filepath = BASE_DIR / f"{case_name}"
    response = requests.get(url)
    return response

def get_case_urls():
    with open(CASE_URL_FILE, "r", encoding="utf-8") as file:
        case_urls = [line.strip() for line in file.readlines()]
    return case_urls

def get_case_info(response):
    soup = BeautifulSoup(response.content, "html.parser")

    main_content = soup.find("container main-content")
    case_content = main_content.find("case-header-horizontal")

    case_image_content = case_content.find("case-image-col")
    # download image

    case_info_content = case_content.find("case-content-col")
    market_accordion = case_info_content.find("market-grid")
    market_items = market_accordion.find_all("market-item")

    case_name = case_info_content.h1.text
    case_price = market_items[0].span.text
    containers_list = containers_data["cases"]
    containers_list.append({"name": case_name, "price": case_price, "items": []})
    containers_data["cases"] = containers_list

    create_case_path(case_name)

    return case_name
    

def create_case_path(case_name):
    case_dir = BASE_DIR / "cases" / case_name
    case_dir.mkdir(exist_ok=True)
    case_assets = case_dir / "assets"
    case_assets.touch(exist_ok=True)
    case_data = case_dir / f"{case_name}.json"
    case_data.touch(exist_ok=True)


def get_case_items(response, case_name):
    case_dir = BASE_DIR / "cases" / case_name
    case_data = case_dir / f"{case_name}.json"

    with open(case_data, "r", encoding="utf-8") as f:
        data = json.load(f)
    cases = data["cases"]
    case_dict = next((item for item in cases if item["name"] == case_name), None)

    soup = BeautifulSoup(response.content, "html.parser")

    main_content = soup.find(class_="case-page-main-row row")
    main_content_items = main_content.find_all(class_="col-lg-4 col-md-6 col-widen text-center")

    case_items_list = []

    for mcitem in main_content_items[1:]:
        item_core = mcitem.div
        item_a = item_core.h2.contents
        item_weapon_name = item_a[0].text
        item_skin_name = item_a[2].text
        item_type_data = item_core.find(class_="nounderline")
        item_type_data_parts = item_type_data.div.p.text.strip().split(" ", 1)
        item_rarity = item_type_data_parts[0]
        item_type = item_type_data_parts[1]
        item_url = item_core.find_all("a")[3].get("href")
        # get_case_item_data(item_url)
        case_items_list.append({
            "weapon": item_weapon_name,
            "name": item_skin_name,
            "rarity": item_rarity,
            "type": item_type,
            "conditions": []
            })

    case_dict["items"] = case_items_list
    return

def get_case_special_items(response, case_name):
    soup = BeautifulSoup(response.content, "html.parser")

    main_content = soup.find(class_="case-page-main-row row")
    main_content_items = main_content.find_all(class_="col-lg-4 col-md-6 col-widen text-center")

    cases_list = []

    for mcitem in main_content_items[0]:
        item_core = mcitem.div
        child_url = item_core.a.get("href")
        container_name = item_core.a.h4.text
        container_price = item_core.a.div.p.text
        container_price = container_price.strip("€")
        cases_list.append({"name": container_name, "price": container_price, "link": child_url, "items": []})
        containers_data["cases"] = cases_list
        #containers_data[container_name] = {"price": container_price, "link": child_url, "items" : []}


    return

def get_case_item_data(response):
    soup = BeautifulSoup(response.content, "html.parser")
    return

def get_case_special_item_data(response):
    soup = BeautifulSoup(response.content, "html.parser")
    return

def scrape():
    case_urls = get_case_urls()
    for case_url in case_urls:
        c_response = get_response(case_url)
        case_name = get_case_info(c_response)
        n_response = get_case_items(c_response, case_name)
        s_response = get_case_special_items(c_response, case_name)
    return


if __name__ == "__main__":
    scrape()