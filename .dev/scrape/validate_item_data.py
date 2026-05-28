import json
from pathlib import Path
from collections import Counter

SCRIPT_DIR = Path(__file__).resolve().parent

DATA_PATH = SCRIPT_DIR / "data"

INPUT_FILE = DATA_PATH / "pre_processed_item_data.json"
OUTPUT_FILE = DATA_PATH / "validation_report.txt"



def is_empty(value):
    return value is None or str(value).strip() == ""


def validate_item(item):
    errors = []

    # -------------------------
    # Top-level required fields
    # -------------------------

    required_fields = [
        "name",
        "skin",
        "quality",
        "type"
    ]

    for field in required_fields:
        if is_empty(item.get(field)):
            errors.append({
                "type": f"missing_{field}",
                "message": f"Missing or empty field: {field}"
            })

    # -------------------------
    # Collection / container logic
    # -------------------------

    collection = item.get("collection")
    container = item.get("container")

    collection_missing = (
        is_empty(collection) or str(collection).strip().lower() == "none"
    )

    container_missing = (
        is_empty(container) or str(container).strip().lower() == "none"
    )

    if collection_missing and container_missing:
        errors.append({
            "type": "missing_collection_and_container",
            "message": "Both collection and container are missing/None"
        })

    # -------------------------
    # Conditions validation
    # -------------------------

    conditions = item.get("conditions", [])

    has_any_stattrak = any(
        c.get("stattrak_price") is not None
        for c in conditions
    )

    if has_any_stattrak:

        for condition in conditions:

            if condition.get("stattrak_price") is None:

                errors.append({
                    "type": "partial_stattrak_missing",
                    "message": f"{condition.get("condition")} missing stattrak price"
                })

    if not conditions:
        errors.append({
            "type": "missing_conditions",
            "message": "Missing conditions list"
        })

        return errors

    for idx, condition_data in enumerate(conditions):

        condition_name = condition_data.get(
            "condition",
            f"UNKNOWN_CONDITION_{idx}"
        )

        regular_price = condition_data.get("regular_price")
        stattrak_price = condition_data.get("stattrak_price")

        # Condition name required
        if is_empty(condition_name):
            errors.append({
                "type": "missing_condition_name",
                "message": f"Condition #{idx} missing condition name"
            })

        # Regular price required
        if is_empty(regular_price):
            errors.append({
                "type": "missing_regular_price",
                "message": f"{condition_name}: regular_price missing/null"
            })

        # Stattrak price cannot exist without regular price
        if (
            stattrak_price is not None
            and not is_empty(stattrak_price)
            and is_empty(regular_price)
        ):
            errors.append({
                "type": "stattrak_without_regular",
                "message": (
                    f"{condition_name}: "
                    f"stattrak_price exists without regular_price"
                )
            })

    return errors


def main():

    with open(INPUT_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    items = data.get("items", [])

    total_items = len(items)
    broken_items = 0

    report_lines = []

    # Track error type counts
    error_counter = Counter()

    for item in items:

        errors = validate_item(item)

        if errors:

            broken_items += 1

            name = item.get("name", "UNKNOWN")
            skin = item.get("skin", "UNKNOWN")

            report_lines.append("=" * 80)
            report_lines.append(f"ITEM: {name} | {skin}")
            report_lines.append("")

            for error in errors:

                error_counter[error["type"]] += 1

                report_lines.append(f"- {error['message']}")

            report_lines.append("")

    # -------------------------
    # Summary section
    # -------------------------

    summary = [
        "=" * 80,
        "VALIDATION SUMMARY",
        "=" * 80,
        f"Total items checked: {total_items}",
        f"Items with errors: {broken_items}",
        f"Items without errors: {total_items - broken_items}",
        "",
        "ERROR TYPE COUNTS",
        "-" * 80
    ]

    if error_counter:

        for error_type, count in error_counter.most_common():
            summary.append(f"{error_type}: {count}")

    else:
        summary.append("No errors found.")

    summary.append("")
    summary.append("=" * 80)
    summary.append("DETAILED ERRORS")
    summary.append("=" * 80)
    summary.append("")

    report_lines = summary + report_lines

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write("\n".join(report_lines))

    print("Validation complete.")
    print(f"Report written to: {OUTPUT_FILE}")


if __name__ == "__main__":
    main()