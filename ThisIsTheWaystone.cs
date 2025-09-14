using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared;
using ImGuiNET;
using ThisIsTheWaystone.UtilityClasses;

namespace ThisIsTheWaystone
{
   
    public class ThisIsTheWaystone : BaseSettingsPlugin<ThisIsTheWaystoneSettings>
    {
        private volatile bool _isProcessing = false;
        private List<WaystoneData> _waystones = new();
        private List<NormalInventoryItem> _paranoiaItems = new();
        private Dictionary<string, List<NormalInventoryItem>> _currencyItems = new();
        private Dictionary<string, int> _lastCurrencyStackIndices = new();

        public override void OnLoad()
        {
            Name = "ThisIsTheWaystone (Refactored)";
        }

        public override bool Initialise()
        {
            try
            {
                // Register configurable keybinds
                Input.RegisterKey(Settings.StartProcessingKey.Value.Key);
                Input.RegisterKey(Settings.EmergencyStopKey.Value.Key);
                
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"ThisIsTheWaystone initialization failed: {ex.Message}", 10);
                return false;
            }
        }

        public override void Tick()
        {
            if (!Settings.Enable.Value) return;

            try
            {
                // Handle configurable keybinds
                if (Input.GetKeyState(Settings.StartProcessingKey.Value.Key))
                {
                    if (Settings.EnableWaystoneProcessing.Value && !_isProcessing)
                    {
                        _isProcessing = true;
                        ProcessWaystones();
                        _isProcessing = false;
                    }
                }

                if (Input.GetKeyState(Settings.EmergencyStopKey.Value.Key))
                {
                    _isProcessing = false;
                    TaskRunner.StopAll();
                    CurrencyOperations.CleanUp();
                }

                // Update inventory data
                UpdateInventoryData();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error in Tick: {ex.Message}", 10);
            }
        }

        private void UpdateInventoryData()
        {
            var inventoryPanel = GameController?.Game?.IngameState?.IngameUi?.InventoryPanel;
            if (inventoryPanel?.IsVisible != true) return;

            var inventoryItems = inventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            
            // Find waystones using clean LINQ
            _waystones = inventoryItems
                .Where(x => x.Item.GetComponent<Base>()?.Name.Contains("Waystone") == true)
                .Select(x => new WaystoneData(x, x.GetClientRect().Center))
                .ToList();

            // Find paranoia items
            _paranoiaItems = inventoryItems
                .Where(x => x.Item.GetComponent<Base>()?.Name == "Liquid Paranoia" && 
                           x.Item.GetComponent<Stack>()?.Size > 0)
                .ToList();

            // Find currency items using dictionary for easy lookup
            _currencyItems = new Dictionary<string, List<NormalInventoryItem>>
            {
                ["CurrencyAddModToMagic"] = inventoryItems.Where(x => x.Item.Path.Contains("CurrencyAddModToMagic") && x.Item.HasComponent<Stack>()).ToList(),
                ["CurrencyUpgradeToRare"] = inventoryItems.Where(x => x.Item.Path.Contains("CurrencyUpgradeToRare") && x.Item.HasComponent<Stack>()).ToList(),
                ["CurrencyUpgradeMagicToRare"] = inventoryItems.Where(x => x.Item.Path.Contains("CurrencyUpgradeMagicToRare") && x.Item.HasComponent<Stack>()).ToList(),
                ["CurrencyAddModToRare"] = inventoryItems.Where(x => x.Item.Path.Contains("CurrencyAddModToRare") && x.Item.HasComponent<Stack>()).ToList()
            };

            // Calculate needs for each waystone
            foreach (var waystone in _waystones)
            {
                waystone.CalculateNeeds();
            }
        }

        private void ProcessWaystones()
        {
            try
            {
                if (!Settings.EnableWaystoneProcessing.Value)
                {
                    return;
                }

                // Check if stash is open - don't process if it is
                var stashPanel = GameController?.Game?.IngameState?.IngameUi?.StashElement;
                if (stashPanel?.IsVisible == true)
                {
                    return; // Do nothing if stash is open
                }

                // Filter processable waystones using clean LINQ
                var processableWaystones = _waystones.Where(w => w.CanProcess && 
                    (w.NeedAugment || w.NeedAlchemy || w.NeedRegal || w.NeedExalt || w.NeedParanoia)).ToList();
                
                if (!processableWaystones.Any())
                {
                    return;
                }

                // Process currency application
                ProcessCurrencyApplication(processableWaystones);

                // Process paranoia application
                if (Settings.UseDistilledParanoia.Value)
                {
                    ProcessParanoiaApplication(processableWaystones);
                }

                // Cleanup
                PerformCleanup();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error in ProcessWaystones: {ex.Message}", 5);
            }
        }

        private void ProcessCurrencyApplication(List<WaystoneData> waystones)
        {
            var waystonesNeedingCurrency = waystones.Where(w => w.NeedAugment || w.NeedAlchemy || w.NeedRegal || w.NeedExalt).ToList();
            
            if (!waystonesNeedingCurrency.Any()) return;

            foreach (var waystone in waystonesNeedingCurrency)
            {
                ProcessWaystoneCurrency(waystone);
            }
        }

        private void ProcessWaystoneCurrency(WaystoneData waystone)
        {
            try
            {
                // Apply Augmentation
                if (waystone.NeedAugment)
                {
                    ApplyCurrency("CurrencyAddModToMagic", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                // Apply Alchemy
                if (waystone.NeedAlchemy)
                {
                    ApplyCurrency("CurrencyUpgradeToRare", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                // Apply Regal
                if (waystone.NeedRegal)
                {
                    ApplyCurrency("CurrencyUpgradeMagicToRare", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                // Apply Exalted Orbs (with real-time checking)
                if (waystone.NeedExalt)
                {
                    ApplyExaltedOrbsWithChecking(waystone);
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error processing currency for {waystone.Name}: {ex.Message}", 5);
            }
        }

        private void ApplyExaltedOrbsWithChecking(WaystoneData waystone)
        {
            int exaltsApplied = 0;
            int maxExalts = waystone.ExaltLeft;

            for (int i = 0; i < maxExalts; i++)
            {
                // Check if waystone is already fully exalted before each exalt
                RecalculateWaystoneState(waystone);
                
                if (waystone.ModifierCount >= 6)
                {
                    break;
                }

                ApplyCurrency("CurrencyAddModToRare", waystone, 1);
                exaltsApplied++;
                    Thread.Sleep(Settings.CurrencyDelay.Value);
            }
        }

        private void RecalculateWaystoneState(WaystoneData waystone)
        {
            try
            {
                // Get fresh data from the inventory item
                var modsComponent = waystone.InventoryItem.Item.GetComponent<Mods>();
                if (modsComponent != null)
                {
                    waystone.ModifierCount = modsComponent.ItemMods?.Count ?? 0;
                    waystone.Rarity = modsComponent.ItemRarity;
                    waystone.IsDistilled = modsComponent.ItemMods?.Any(m => m.DisplayName.Contains("InstilledMapDelirium")) ?? false;

                    // Recalculate needs based on new state
                    waystone.CalculateNeeds();
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error recalculating waystone state: {ex.Message}", 5);
            }
        }

        private void ProcessParanoiaApplication(List<WaystoneData> waystones)
        {
            var waystonesNeedingParanoia = waystones.Where(w => w.NeedParanoia && !w.IsDistilled).ToList();
            
            if (!waystonesNeedingParanoia.Any()) return;

            // Process all waystones in a single distillation session
            ProcessAllWaystonesInDistillation(waystonesNeedingParanoia);
        }

        private void ProcessAllWaystonesInDistillation(List<WaystoneData> waystones)
        {
            try
            {
                // Find paranoia with at least 3 charges
                var paranoia = _paranoiaItems.FirstOrDefault(p => p.Item.GetComponent<Stack>()?.Size >= 3);
                if (paranoia == null)
                {
                    return;
                }

                // Open distillation window
                CurrencyOperations.UseItemRightClick(paranoia);
                Thread.Sleep(350);

                // Transfer 3 paranoias to distillation window
                for (int i = 0; i < 3; i++)
                {
                    CurrencyOperations.CtrlClickItem(paranoia);
                    Thread.Sleep(200);
                }

                // Process each waystone in the distillation window
                foreach (var waystone in waystones)
                {
                    // Add waystone to distillation window
                    CurrencyOperations.CtrlClickItem(waystone.InventoryItem);
                    Thread.Sleep(200);

                    // Click instill button (using configurable position)
                    var instillPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(Settings.InstillButtonX.Value, Settings.InstillButtonY.Value);
                    CurrencyOperations.ClickAtPosition(instillPos);
                    Thread.Sleep(750);

                    // Retrieve the distilled waystone back to inventory
                    var waystoneInDistillationPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(Settings.DistillWaystoneX.Value, Settings.DistillWaystoneY.Value);
                    Input.SetCursorPos(waystoneInDistillationPos);
                    Thread.Sleep(100);
                    Input.KeyDown(Keys.ControlKey);
                    Thread.Sleep(50);
                    Input.Click(MouseButtons.Left);
                    Thread.Sleep(60);
                    Input.KeyUp(Keys.ControlKey);
                    Thread.Sleep(350);
                }

                // No need to press Escape - the distillation window will close automatically
                // or the last waystone Ctrl+click will handle it
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error processing waystones in distillation: {ex.Message}", 5);
            }
        }

        private void ApplyCurrency(string currencyPath, WaystoneData waystone, int count = 1)
        {
            var currency = GetCurrencyItem(currencyPath);
            if (currency == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                CurrencyOperations.UseCurrencyOnItem(currency, waystone.InventoryItem);
                Thread.Sleep(Settings.CurrencyDelay.Value);
            }
        }

        private NormalInventoryItem GetCurrencyItem(string currencyPath)
        {
            if (!_currencyItems.ContainsKey(currencyPath)) return null;
            
            return _currencyItems[currencyPath]
                .FirstOrDefault(x => x.Item.GetComponent<Stack>()?.Size > 0);
        }

        private void ShowCurrencyCount(string displayName, string currencyPath)
        {
            if (_currencyItems.ContainsKey(currencyPath))
            {
                var count = _currencyItems[currencyPath].Sum(x => x.Item.GetComponent<Stack>()?.Size ?? 0);
                ImGui.Text($"{displayName}: {count}");
            }
            else
            {
                ImGui.Text($"{displayName}: 0");
            }
        }

        private void PerformCleanup()
        {
            try
            {
                // Press Escape to close any open windows
                CurrencyOperations.PressKey(Keys.Escape);
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error during cleanup: {ex.Message}", 5);
            }
        }

        public override void Render()
        {
            if (!Settings.Enable.Value) return;

            // Only show GUI when inventory is open
            var inventoryPanel = GameController?.Game?.IngameState?.IngameUi?.InventoryPanel;
            if (inventoryPanel?.IsVisible != true) return;

            // Check if stash is open
            var stashPanel = GameController?.Game?.IngameState?.IngameUi?.StashElement;
            bool isStashOpen = stashPanel?.IsVisible == true;

            ImGui.SetNextWindowPos(new Vector2(Settings.PositionX.Value, Settings.PositionY.Value), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("ThisIsTheWaystone", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "ThisIsTheWaystone");
                ImGui.Separator();

                // Show stash warning if stash is open
                if (isStashOpen)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "⚠️ CLOSE YOUR FRICKIN' STASH! ⚠️");
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Plugin disabled while stash is open");
                    ImGui.End();
                    return;
                }

                ImGui.Text($"Waystones Found: {_waystones.Count}");
                ImGui.Text($"Paranoia Items: {_paranoiaItems.Count}");
                
                // Show currency counts with proper names
                ImGui.Separator();
                ImGui.Text("Currency Counts:");
                ShowCurrencyCount("Orb of Augmentation", "CurrencyAddModToMagic");
                ShowCurrencyCount("Orb of Alchemy", "CurrencyUpgradeToRare");
                ShowCurrencyCount("Regal Orb", "CurrencyUpgradeMagicToRare");
                ShowCurrencyCount("Exalted Orb", "CurrencyAddModToRare");
                
                if (_isProcessing)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "PROCESSING...");
                }
                
                if (ImGui.Button("Process Waystones"))
                {
                    if (!_isProcessing)
                    {
                        _isProcessing = true;
                        ProcessWaystones();
                        _isProcessing = false;
                    }
                }

                // Show waystone details
                if (ImGui.TreeNode("Waystone Details"))
                {
                    foreach (var waystone in _waystones)
                    {
                        var color = waystone.CanProcess ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1);
                        ImGui.TextColored(color, $"{waystone.Name} - {waystone.GetNeedsSummary()}");
                    }
                    ImGui.TreePop();
                }

                // Show mouse coordinates if enabled
                if (Settings.ShowMouseCoordinates.Value)
                {
                    ImGui.Separator();
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Mouse Coordinates:");
                    
                    var mousePos = Input.MousePosition;
                    ImGui.Text($"X: {mousePos.X:F0}, Y: {mousePos.Y:F0}");
                }
                
                ImGui.End();
            }
        }
    }
}
