# DateEverything-WhoIsYou

## Overview

DateEverything-WhoIsYou is a BepInEx/Harmony plugin for Date Everything that displays the name of interactable objects as an overlay before you commit to an interaction.

## Features

- Shows the name of the object you're looking at, in a clean overlay at the top of the screen.
- Only displays when the Dateviators are equipped.
- Overlay can be toggled on/off with a configurable key (default: `BackQuote`).
- Optionally displays names for objects you haven't met yet (configurable).
- Debug mode for extra diagnostic info in the log.
- Overlay appearance is dynamic and adapts to screen resolution.

## Configuration

All options are configurable via the plugin's config file:

- **EnabledByDefault**: Enable overlay at game start (default: true)
- **ToggleKey**: Key to toggle overlay display (default: BackQuote)
- **EnableDebug**: Log extra diagnostic info (default: false)
- **DisplayUnmetNames**: Show overlay for unmet objects (default: false)

## Example Overlay

When looking at an interactable object with Dateviators equipped, you'll see something like:

<img width="50%" height="50%" alt="Overlay Example" src="https://github.com/user-attachments/assets/6a97adef-ca5a-4056-a8d2-3a9697872277" />

If the object is unmet and `DisplayUnmetNames` is false, no overlay will appear.

You can toggle the overlay at any time using your configured key.

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest) for your game.
2. Place `WhoIsYou.dll` in your BepInEx plugins folder.
3. Launch the game and configure options in `BepInEx/config/WhoIsYou.cfg` as desired.

## Development

See `WhoIsYou/Plugin.cs` for source code and patch logic. Contributions and suggestions welcome!
