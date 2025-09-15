using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace ThisIsTheWaystone
{
    public class ThisIsTheWaystoneSettings : ISettings
    {
        [Menu("Enable Plugin", "Enable or disable the ThisIsTheWaystone plugin")]
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Position X", "X position of the ThisIsTheWaystone window")]
        public RangeNode<int> PositionX { get; set; } = new RangeNode<int>(50, 0, 2000);

        [Menu("Position Y", "Y position of the ThisIsTheWaystone window")]
        public RangeNode<int> PositionY { get; set; } = new RangeNode<int>(50, 0, 2000);

        [Menu("Show Debug Info", "Show additional debug information")]
        public ToggleNode ShowDebug { get; set; } = new ToggleNode(false);

        [Menu("Waystone Processing", "Enable automatic waystone processing")]
        public ToggleNode EnableWaystoneProcessing { get; set; } = new ToggleNode(true);

        [Menu("Process Normal Waystones", "Process normal rarity waystones")]
        public ToggleNode ProcessNormalWaystones { get; set; } = new ToggleNode(true);

        [Menu("Process Magic Waystones", "Process magic rarity waystones")]
        public ToggleNode ProcessMagicWaystones { get; set; } = new ToggleNode(true);

        [Menu("Process Rare Waystones", "Process rare rarity waystones")]
        public ToggleNode ProcessRareWaystones { get; set; } = new ToggleNode(true);

        [Menu("Skip Distilled Waystones", "Skip waystones that are already distilled")]
        public ToggleNode SkipDistilledWaystones { get; set; } = new ToggleNode(true);

        [Menu("Use Distilled Paranoia", "Enable distilled paranoia processing after currency application")]
        public ToggleNode UseDistilledParanoia { get; set; } = new ToggleNode(true);

        [Menu("Currency Application Delay", "Delay between currency applications (ms)")]
        public RangeNode<int> CurrencyDelay { get; set; } = new RangeNode<int>(200, 50, 1000);

        [Menu("Waystone Processing Delay", "Delay between waystone processing (ms)")]
        public RangeNode<int> WaystoneDelay { get; set; } = new RangeNode<int>(500, 100, 2000);

        [Menu("Instill Button X", "X position of the instill button")]
        public RangeNode<int> InstillButtonX { get; set; } = new RangeNode<int>(935, 0, 2000);

        [Menu("Instill Button Y", "Y position of the instill button")]
        public RangeNode<int> InstillButtonY { get; set; } = new RangeNode<int>(868, 0, 2000);

        [Menu("Distill UI Waystone X", "X position of waystone in distill UI")]
        public RangeNode<int> DistillWaystoneX { get; set; } = new RangeNode<int>(935, 0, 2000);

        [Menu("Distill UI Waystone Y", "Y position of waystone in distill UI")]
        public RangeNode<int> DistillWaystoneY { get; set; } = new RangeNode<int>(460, 0, 2000);


        [Menu("Show Waystone Requirements", "Show detailed waystone requirements in UI")]
        public ToggleNode ShowWaystoneRequirements { get; set; } = new ToggleNode(true);

        [Menu("Mouse Coordinate Display", "Show current mouse coordinates in the ThisIsTheWaystone window")]
        public ToggleNode ShowMouseCoordinates { get; set; } = new ToggleNode(false);

        [Menu("Start Processing Key", "Key to start waystone processing")]
        public HotkeyNodeV2 StartProcessingKey { get; set; } = Keys.D8;

        [Menu("Emergency Stop Key", "Key to emergency stop processing")]
        public HotkeyNodeV2 EmergencyStopKey { get; set; } = Keys.D9;
    }
}
