# ThisIsTheWaystone Plugin for ExileCore2

🎯 **Automate your waystone processing in Path of Exile!**

A comprehensive plugin that automatically processes waystones by applying the correct currency and distilling them with Liquid Paranoia or Diluted Liquid Greed. Perfect for players who want to streamline their waystone crafting workflow.

## ✨ Key Features

- **🤖 Smart Processing**: Automatically applies Augmentation, Alchemy, Regal, and Exalted Orbs in the correct order
- **🧪 Dual Distillation**: Support for both Liquid Paranoia and Diluted Liquid Greed
- **📊 Real-time Display**: Live waystone and currency counts in a resizable window
- **🛡️ Safety First**: Stash detection, emergency stop, and comprehensive error handling
- **⚙️ Fully Configurable**: Customizable settings, hotkeys, and UI positions
- **🔄 Batch Processing**: Handles multiple waystones efficiently in one session
- **✅ Item Validation**: Ensures correct items are selected at each step

## 🚀 Quick Start

### Prerequisites
- Path of Exile
- [ExileCore2](https://github.com/ExileCore2/ExileCore2) (download from GitHub)
- .NET 8.0 SDK (download from Microsoft)
- Visual Studio 2022 Community (free)

### Installation
1. **Download** this plugin
2. **Extract** the `ThisIsTheWaystone` folder
3. **Copy** it to your ExileCore2 `Plugins\Source\` directory
4. **Open** `ThisIsTheWaystone.sln` in Visual Studio
5. **Build** the solution (Ctrl+Shift+B)
6. **Launch** ExileCore2 and enable the plugin

### Usage
1. **Enable** the plugin in ExileCore2 settings
2. **Open** your inventory in-game
3. **Press D8** to start processing waystones
4. **Press D9** for emergency stop

## 🎮 How It Works

### For a 1-Modifier Magic Waystone:
1. **Apply Augmentation** → adds 1 more modifier
2. **Apply Regal Orb** → upgrades to rare
3. **Apply 3 Exalted Orbs** → adds 3 more modifiers (total: 6)
4. **Distill with Paranoia/Greed** → adds delirium effect

### Batch Processing:
- Processes all waystones in your inventory
- Applies currency to all waystones first
- Then distills all waystones in one session
- Handles multiple emotion item stacks automatically

## ⚙️ Settings

### Basic Settings
- **Enable Plugin**: Toggle the plugin on/off
- **Position X/Y**: Adjust window position
- **Show Debug Info**: Display additional information

### Processing Settings
- **Process Normal/Magic/Rare Waystones**: Choose which rarities to process
- **Skip Distilled Waystones**: Skip already processed waystones
- **Currency Application Delay**: Delay between currency applications (ms)

### Distillation Settings
- **Use Distilled Paranoia**: Enable Liquid Paranoia distillation
- **Use Diluted Liquid Greed**: Enable Diluted Liquid Greed distillation
- **Use No Distilled Emotions**: Disable all distillation

### UI Settings
- **Instill Button X/Y**: Position of instill button in distillation UI
- **Distill UI Waystone X/Y**: Position of waystone in distillation UI
- **Show Mouse Coordinates**: Display mouse position for UI setup

### Hotkeys
- **Start Processing Key**: Default D8
- **Emergency Stop Key**: Default D9

## 🛡️ Safety Features

- **Stash Detection**: Won't process when stash is open
- **Emergency Stop**: Instantly halt all processing
- **Item Validation**: Ensures correct items are selected
- **Error Recovery**: Comprehensive error handling and logging
- **Force Stop**: Complete reset of plugin state

## 📋 What You Need

### Required Items
- **Waystones** in your inventory
- **Currency items**: Augmentation, Alchemy, Regal, Exalted Orbs
- **Distillation materials**: Liquid Paranoia OR Diluted Liquid Greed

### Important Notes
- ⚠️ **Close your stash** before using the plugin
- 📦 Plugin only works when **inventory is open**
- 🎯 Waystones are processed in **inventory order**
- 🛑 Use **emergency stop (D9)** if something goes wrong

## 🔧 Troubleshooting

### Common Issues
- **"ExileCore2 could not be found"**: Make sure ExileCore2.dll, GameOffsets2.dll, and ItemFilterLibrary.dll are in your ExileCore2 main directory
- **Build errors**: Ensure you have .NET 8.0 SDK installed
- **Plugin not working**: Check that your stash is closed and inventory is open

### Getting Help
- Check the `INSTALLATION_GUIDE.txt` for detailed setup instructions
- Report issues on the plugin's GitHub page
- Make sure you're using the latest version of ExileCore2

## 🎉 Features in Detail

### Smart Currency Application
- Automatically determines what each waystone needs
- Applies currency in the correct order
- Handles different waystone rarities appropriately
- Real-time validation of waystone states

### Efficient Distillation
- Opens distillation window once per batch
- Transfers 1 emotion item per Ctrl+click
- Processes all waystones in sequence
- Handles multiple emotion item stacks

### User-Friendly Interface
- Real-time waystone and currency counting
- Radio buttons for easy emotion type selection
- Configurable UI positions
- Mouse coordinate display for setup

## 🚀 Future Enhancements

- Additional waystone types support
- Stash waystone processing
- Custom processing rules
- Export functionality
- Sound notifications
- Advanced filtering options

---

**Happy waystone processing!** 🎯✨

*This plugin is designed to make your Path of Exile waystone crafting experience smoother and more efficient. Always use responsibly and ensure you have the necessary materials before processing.*