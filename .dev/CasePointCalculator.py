import json

RARITY_ODDS = {
    "Mil-Spec": 0.7992,
    "Restricted": 0.1598,
    "Classified": 0.0320,
    "Covert": 0.0064,
    "Special": 0.0026
}


def get_average_item_value(item):
    prices = []

    for condition in item["CONDITIONS"]:
        regular = condition.get("regular_price")

        if regular is not None and regular > 0:
            prices.append(regular)

    if not prices:
        return 0

    return sum(prices) / len(prices)


def calculate_case_score(case_data):

    case_price = case_data["PRICE"]
    items = case_data["ITEMS"]

    rarity_groups = {}

    highest_item_value = 0
    profitable_items = 0

    for item in items:

        rarity = item["RARITY"]

        avg_value = get_average_item_value(item)

        highest_item_value = max(highest_item_value, avg_value)

        if avg_value > case_price:
            profitable_items += 1

        rarity_groups.setdefault(rarity, []).append(avg_value)

    expected_value = 0

    for rarity, probability in RARITY_ODDS.items():

        values = rarity_groups.get(rarity, [])

        if not values:
            continue

        rarity_average = sum(values) / len(values)

        expected_value += rarity_average * probability

    ev_ratio = expected_value / case_price if case_price > 0 else 0

    profit_chance = profitable_items / len(items) if items else 0

    jackpot_ratio = highest_item_value / case_price if case_price > 0 else 0

    # ---- SCORING ----

    ev_score = min(ev_ratio, 2.0) / 2.0

    jackpot_score = min(jackpot_ratio, 100.0) / 100.0

    final_score = (
        ev_score * 0.60 +
        jackpot_score * 0.25 +
        profit_chance * 0.15
    ) * 10

    final_score = round(final_score, 2)

    return {
        "expected_value": round(expected_value, 2),
        "ev_ratio": round(ev_ratio, 3),
        "highest_item_value": round(highest_item_value, 2),
        "profit_chance": round(profit_chance * 100, 2),
        "score": final_score
    }


def main():

    with open("cases.json", "r", encoding="utf-8") as f:
        data = json.load(f)

    results = []

    for case in data["cases"]:

        score_data = calculate_case_score(case)

        results.append({
            "id": case["ID"],
            "name": case["NAME"],
            **score_data
        })

    results.sort(key=lambda x: x["score"], reverse=True)

    with open("case_scores.json", "w", encoding="utf-8") as f:
        json.dump(results, f, indent=4)

    print(f"Scored {len(results)} cases")


if __name__ == "__main__":
    main()