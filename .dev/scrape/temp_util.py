from pathlib import Path
import xml.etree.ElementTree as ET

SCRIPT_DIR = Path(__file__).resolve().parent
RAW_DATA_PATH = SCRIPT_DIR / "raw_data"

def parse_xml():
    xml_path = RAW_DATA_PATH / "steamanalyst_sitemap.xml"
    with open(xml_path) as f:
        xml_data = f.read()

    root =  ET.fromstring(xml_data)

    namespace = {
        "ns": "http://www.sitemaps.org/schemas/sitemap/0.9"
    }

    cond_list = ["factory-new","minimal-wear","field-tested","well-worn","battle-scarred"]
    kw_list = ["sticker", "package", "graffiti-", "challengers", "legends", "patch", "charm"]
    weapon_list = ["karambit", "bayonet", "m9-bayonet", "butterfly-knife", "flip-knife", "gut-knife", "huntsman-knife", "falchion-knife", "shadow-daggers", "bowie-knife", "talon-knife", "ursus-knife", "navaja-knife", "stiletto-knife", "classic-knife", "skeleton-knife", "nomad-knife", "survival-knife", "paracord-knife", "kukri-knife", "sport-gloves", "driver-gloves", "moto-gloves", "specialist-gloves", "hand-wraps", "bloodhound-gloves", "hydra-gloves", "ak-47", "m4a4", "m4a1-s", "aug", "sg-553", "famas", "galil-ar", "desert-eagle", "usp-s", "glock-18", "p2000", "five-seven", "tec-9", "cz75-auto", "p250", "dual-berettas", "r8-revolver", "mp9", "mac-10", "mp7", "mp5-sd", "ump-45", "p90", "pp-bizon", "awp", "ssg-08", "scar-20", "g3sg1", "mag-7", "nova", "xm1014", "sawed-off", "m249", "negev"]

    skin_locs_list = []
    case_locs_list = []
    collection_locs_list = []
    package_locs_list = []
    sticker_locs_list = []
    graffiti_locs_list = []

    for loc in root.findall(".//ns:loc", namespace):
        link = loc.text
        if "/skin/" in link:
            if "package" in link:
                package_locs_list.append(link)
            if "sticker" in link and not any(x in link for x in ["slab", "sealed"]):
                sticker_locs_list.append(link)
            if "graffiti-" in link:
                graffiti_locs_list.append(link)
            if not any(cond in link for cond in cond_list) and not any(kw in link for kw in kw_list) and any(weapon in link for weapon in weapon_list):
                skin_locs_list.append(link)
        elif "/case/" in link:
            case_locs_list.append(link)
        elif "/collection/" in link:
            collection_locs_list.append(link)

    save_to_path("skin", skin_locs_list)
    save_to_path("case", case_locs_list)
    save_to_path("collection", collection_locs_list)
    save_to_path("package", package_locs_list)
    save_to_path("sticker", sticker_locs_list)
    save_to_path("graffiti", graffiti_locs_list)

def save_to_path(name, list):
    path = RAW_DATA_PATH / f"{name}_locs.txt"
    path.touch(exist_ok=True)

    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(list))

def get_types_from_xml():
    xml_path = RAW_DATA_PATH / "steamanalyst_sitemap.xml"
    with open(xml_path) as f:
        xml_data = f.read()

    root =  ET.fromstring(xml_data)

    namespace = {
        "ns": "http://www.sitemaps.org/schemas/sitemap/0.9"
    }

    str_list = []

    for loc in root.findall(".//ns:loc", namespace):
        loc_strip = loc.text.replace("https://www.steamanalyst.com/", "")
        str_left = loc_strip.split("/")[0]
        str_list.append(str_left)
    str_set = set(str_list)
    print("\n".join(str_set))

parse_xml()