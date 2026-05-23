from bs4 import BeautifulSoup

local_html_path = "D:/UnityProjects/CS2SIM/.dev/Dreams & Nightmares Case Skins (CS2 & CS_GO) - Dreams & Nightmares Case Price & Stats.html"

with open(local_html_path, 'r', encoding='utf-8') as file:
    html_content = file.read()

soup = BeautifulSoup(html_content, 'html.parser')

title = soup.find('title').text
print(title)