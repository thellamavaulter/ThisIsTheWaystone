# ThisIsTheWaystone
A waystone juicing plugin for exilecore2\poe2


ThisIsTheWaystone Plugin - Installation Guide
==============================================

REQUIREMENTS:
- ExileCore2 installed and working
- .NET 8.0 SDK

INSTALLATION STEPS:
1. Extract the ThisIsTheWaystone-Plugin.zip file
2. Copy the entire "ThisIsTheWaystone" folder to your ExileCore2 Plugins\Source\ directory
3. The plugin will appear in your ExileCore2 plugins list

***OPTIONALLY***
copy the github link into plugin updater's 'Add' input line:
https://github.com/thellamavaulter/ThisIsTheWaystone


USAGE:
1. Enable the plugin in ExileCore2 settings
2. Open your inventory in-game
3. Press D8 to start processing waystones
4. Press D9 for emergency stop

SETTINGS:
- All settings are configurable through ExileCore2's settings menu
- "Waystone Processing" is enabled by default
- Adjust UI positions and delays as needed

## How It Works

1. **Detection**: Scans your inventory for waystones and currency items
2. **Analysis**: Determines what each waystone needs based on its current state
3. **Currency Application**: Applies the appropriate currency in the correct order
4. **Paranoia Distillation**: Uses Liquid Paranoia to distill processed waystones
5. **Real-time Updates**: Continuously monitors and updates waystone states

## Future Enhancements

This is a refactored version with improved performance and maintainability. Planned improvements include:
- Additional waystone types support
- Stash waystone processing
- Custom processing rules
- Export functionality
- Sound notifications
- Advanced filtering options
