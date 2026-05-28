
from bs4 import BeautifulSoup
from pathlib import Path

scrape_dir = Path(__file__).resolve().parent
html_dir = scrape_dir / "HTML"
raw_data_dir = scrape_dir / "raw_data"

cases_dropdown = html_dir / "cases_dropdown.html"
case_urls = raw_data_dir / "case_urls.txt"

with open(cases_dropdown, "r", encoding="utf-8") as file:
    cases_dd = file.read()
cases_dd_soup = BeautifulSoup(cases_dd, "html.parser")

items_li = cases_dd_soup.find_all(class_="item")

links = []

for item in items_li:
    link = item.find("a").get("href")
    links.append(link)

with open(case_urls, "w", encoding="utf-8") as file:
    file.write("\n".join(links))