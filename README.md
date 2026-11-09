# CS2SIM

## UI themes

Profiles live in `Assets/Resources/ThemeProfiles`; create a `ThemeProfile`
asset there to add a preset. Each profile defines layered surfaces, text,
accent, button, navigation, and semantic colors.

Theme application is explicit: add `ThemeElement` to a UI Graphic in its
prefab or scene, then choose its `ThemeColorRole`. Unmarked UI is not modified.
The shared UI prefab assigns `NavigationSurface` to both the top bar and side
panel, keeping them consistent while distinct from the screen background.

To let players choose a profile, wire a static Button's `OnClick` event to
`SettingsManager.SelectTheme` and enter a profile ID (for example
`midnight-amethyst`) as the string argument. The selection is saved between
launches and applied to all active, explicitly assigned theme elements.
 
