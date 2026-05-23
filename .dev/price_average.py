import json
import os.path

main_path = os.path.dirname(os.path.realpath(__file__))
root_path = os.path.abspath(os.path.join(main_path, os.pardir))

case_id = "B_CASE"
case_file = os.path.join(root_path, f"Assets/StreamingAssets/CaseDataJSON/{case_id}.json")



def get_price_average_from_case_data():
    with open(case_file, "r") as file:
        case_data = json.load(file)
    
    case = case_data["cases"][0]
    case_items = case["ITEMS"]

    cumulative_price = 0
    item_count = 0
    for item in case_items:
        cumulative_price += item["PRICE"]
        item_count += 1
    
    avg_price = cumulative_price / item_count
    print("Average item price: {:.2f}".format(round(avg_price, 2)))

if __name__ == "__main__":
    get_price_average_from_case_data()