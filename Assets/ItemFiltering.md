# Item filtering

Add `ItemFilterButton` to a UI Button in the Inventory, Market, Upgrader, or
Roulette scene. It subscribes to the button automatically and finds the active
filtering screen in the same scene when clicked; no Inspector On Click binding
or reference to the persistent `InventoryManager` is needed. The Inventory
scene's `FilterPanelBtn` is already configured. In other scenes, remove any
existing On Click binding to `OpenFilters` when adding the component to avoid
opening twice.

The scene must contain exactly one active filtering screen. Inventory uses
`InventoryScreen` on its Canvas, while the other scenes use their respective
scene managers. The inventory data manager is created in the Main Menu and must
be present before opening Inventory filters. Missing or ambiguous screens and a
missing inventory manager are reported in the Unity Console.

The screen opens the `Resources/ItemFilterPopup` prefab variant on the root
canvas; do not instantiate it directly, because the controller supplies its
filter state and callback. Edit `Assets/Prefabs/FilterPanel.prefab` to change the
layout, controls, theme roles, and the inactive `Option template` used for
item-dependent values. The named option containers, StatTrak toggles, action
buttons, and template references on `ItemFilterPopup` must remain assigned.
Changes to the base prefab flow through to the Resources variant.

Options are generated from the items available to the screen. Conditions and
rarities are ordered best to worst; unrecognized values appear afterward in
alphabetical order. Types and guns are alphabetical. Multiple selected
values in one category match any of those values; different categories must all
match. An empty category imposes no restriction. The two StatTrak choices can
both be selected. Filtering applies immediately, composes with sorting and
existing search, price, balance, and eligibility restrictions, and resets the
display to the first page. Clear filters removes all selections and closes the
popup. Filters last for the lifetime of the scene; changing tabs or closing the
popup retains them.
In Roulette, filtering only hides items from the inventory list: already selected
items remain selected and can still be removed from the selected-items list.
While the popup is open, mouse-wheel page navigation is disabled in that scene,
so scrolling its options cannot also change the underlying page.
