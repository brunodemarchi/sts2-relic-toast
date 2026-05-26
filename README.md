# Relic Toast

[Português (Brasil)](README.pt-BR.md)

Relic Toast adds a small, configurable popup whenever you obtain a relic in Slay the Spire 2.

![Relic Toast example](docs/assets/relicexample.gif)

The popup uses the relic's current in-game name, description, rarity, and artwork, so it follows your selected language and works with character-specific relics.

## Features

- Shows a toast when you obtain a relic.
- Uses the current game language automatically.
- Displays relic artwork, name, rarity, and description.
- Lets you choose position, scale, offsets, animation style, and timing.
- Queues multiple relics cleanly instead of stacking popups on top of each other.
- Includes a test button and test relic picker in the settings menu.
- Local only.

## Requirements

- Slay the Spire 2 `v0.105.0` or newer.
- BaseLib `v3.1.2` or newer.

## Install

1. Download the latest `RelicToast` release zip from the GitHub Releases page.
2. Close Slay the Spire 2.
3. Extract the zip into your Slay the Spire 2 `mods` folder.

The installed folder should look like this:

```text
Slay the Spire 2/
  mods/
    RelicToast/
      RelicToast.dll
      RelicToast.json
```

Launch the game with BaseLib and Relic Toast enabled.

## Settings

Open the BaseLib mod settings menu and select Relic Toast.

Available settings:

- `Enabled`
- `Position`
- `Scale`
- `Offset X`
- `Offset Y`
- `Animation In`
- `Animation Out`
- `Time On Screen`
- `In Duration`
- `Out Duration`
- `Queue Delay`
- `Test Relic`
- `Test Toast`

Positions:

- `TopLeft`
- `TopCenter`
- `TopRight`
- `BottomLeft`
- `BottomCenter`
- `BottomRight`

Animations:

- `None`
- `Fade`
- `SlideLeftRight`
- `SlideRightLeft`
- `SlideTopBottom`
- `SlideBottomTop`

## Build From Source

Install the .NET 9 SDK, then build:

```powershell
dotnet build RelicToast.sln -c Release
```

By default, the project looks for Slay the Spire 2 in the standard Steam install location:

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
```

If your game is installed somewhere else:

```powershell
dotnet build RelicToast.sln -c Release -p:Sts2Path="D:\SteamLibrary\steamapps\common\Slay the Spire 2"
```

The files to install are produced here:

```text
bin/Release/RelicToast.dll
bin/Release/RelicToast.json
```

## Troubleshooting

If the toast does not appear:

- Make sure BaseLib is installed and enabled.
- Make sure Relic Toast is installed in `mods/RelicToast/`.
- Restart the game after replacing `RelicToast.dll`.
- Try the `Test Toast` button in the Relic Toast settings.
- Check the log file:

```text
%APPDATA%\SlayTheSpire2\RelicToast.log
```

If Windows will not let you replace `RelicToast.dll`, the game is probably still running. Close Slay the Spire 2 first.

## Notes

Slay the Spire 2 is in active development, and modding APIs can change. If a game or BaseLib update breaks Relic Toast, please open an issue with your game version, BaseLib version, and the log file if available.

## Development Note

This project was built with AI coding assistance.
