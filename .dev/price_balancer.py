import json

# Define rarity multipliers for item price adjustment
rarity_multipliers = {
    "Common": 1.0,
    "Uncommon": 1.5,
    "Rare": 2.5,
    "Epic": 5.0,
    "Legendary": 10.0
}

def rebalance_prices(data):
    # Loop over each crate to adjust item prices and recalculate crate prices
    for crate in data['crates']:
        crate_total_value = 0
        item_count = len(crate['items'])

        # Adjust item prices based on rarity
        for item in crate['items']:
            rarity = item['rarity']
            base_price = item['price']  # Original base price from JSON
            multiplier = rarity_multipliers.get(rarity, 1.0)  # Default to 1.0 if rarity not found
            adjusted_price = base_price * multiplier
            item['price'] = round(adjusted_price, 2)  # Update item price with adjusted value
            crate_total_value += adjusted_price

        # Calculate average item price in the crate and apply a 10% discount for the crate price
        average_item_price = crate_total_value / item_count if item_count else 0
        crate['price'] = round(average_item_price * 0.9, 2)  # Apply 10% discount

    return data

# Load the original JSON data
with open('D:/Unity Projects/RPGSIM/DEV/old_rpg_crate_data.json', 'r') as file:
    data = json.load(file)

# Rebalance the prices
rebalanced_data = rebalance_prices(data)

# Save the rebalanced data to a new JSON file
with open('D:/Unity Projects/RPGSIM/DEV/rpg_crate_data.json', 'w') as file:
    json.dump(rebalanced_data, file, indent=4)

print("Rebalancing complete. Saved to 'rebalanced_rpg_crate_data.json'")
