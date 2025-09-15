# ThisIsTheWaystone Plugin for ExileCore2

A comprehensive waystone processing plugin that automates currency application and paranoia distillation for waystones in Path of Exile.

## Features

- **Waystone Processing**: Automatically processes waystones with the appropriate currency
- **Currency Application**: Applies Augmentation, Alchemy, Regal, and Exalted Orbs as needed
- **Paranoia Distillation**: Automatically distills waystones with Liquid Paranoia
- **Real-time Display**: Shows waystone counts and currency availability in a resizable window
- **Inventory Detection**: Only displays counts when inventory is open
- **Settings Integration**: Fully integrated with ExileCore2 settings system
- **Enable/Disable**: Can be toggled on/off from ExileCore2 UI
- **Keybind Support**: Configurable hotkeys for processing and emergency stop

## Installation

1. Copy the `ThisIsTheWaystone` folder to your ExileCore2 `Plugins\Source\` directory
2. Build the plugin using Visual Studio or your preferred .NET 8.0 build environment
3. The compiled plugin will appear in your ExileCore2 plugins list

## Usage

1. Enable the plugin in ExileCore2 settings
2. Open your inventory in-game
3. The ThisIsTheWaystone window will appear showing current waystone and currency counts
4. Use the configured hotkey (default: D8) to start processing waystones
5. Use the emergency stop key (default: D9) to halt processing if needed

## Settings

- **Enable Plugin**: Toggle the plugin on/off
- **Position X/Y**: Adjust the window position on screen
- **Waystone Processing**: Enable automatic waystone processing
- **Process Normal/Magic/Rare Waystones**: Control which rarity waystones to process
- **Skip Distilled Waystones**: Skip waystones that are already distilled
- **Use Distilled Paranoia**: Enable paranoia distillation after currency application
- **Currency Application Delay**: Delay between currency applications (ms)
- **Waystone Processing Delay**: Delay between waystone processing (ms)
- **Instill Button X/Y**: Position of the instill button in distillation UI
- **Distill UI Waystone X/Y**: Position of waystone in distillation UI
- **Show Waystone Requirements**: Display detailed waystone requirements in UI
- **Mouse Coordinate Display**: Show current mouse coordinates for UI positioning
- **Start Processing Key**: Hotkey to start waystone processing
- **Emergency Stop Key**: Hotkey to emergency stop processing

## Building

The plugin requires:
- .NET 8.0 SDK
- ExileCore2 framework
- Visual Studio 2022 or compatible IDE

Build the project and the compiled DLL will be ready for use with ExileCore2.

## How It Works

1. **Detection**: Scans your inventory for waystones and currency items
2. **Analysis**: Determines what each waystone needs based on its current state
3. **Currency Application**: Applies the appropriate currency in the correct order
4. **Paranoia Distillation**: Uses Liquid Paranoia to distill processed waystones
5. **Real-time Updates**: Continuously monitors and updates waystone states

## Safety Features

- **Stash Detection**: Automatically disables processing when stash is open
- **Emergency Stop**: Hotkey to immediately halt all processing
- **Error Handling**: Comprehensive error handling and logging
- **State Validation**: Real-time validation of waystone states before processing

## Future Enhancements

This is a refactored version with improved performance and maintainability. Planned improvements include:
- Additional waystone types support
- Stash waystone processing
- Custom processing rules
- Export functionality
- Sound notifications
- Advanced filtering options