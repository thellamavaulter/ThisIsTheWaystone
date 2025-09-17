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
        private volatile bool _forceStop = false;
        private List<WaystoneData> _waystones = new();
        private List<NormalInventoryItem> _paranoiaItems = new();
        private List<NormalInventoryItem> _greedItems = new();
        private Dictionary<string, List<NormalInventoryItem>> _currencyItems = new();
        private Dictionary<string, int> _lastCurrencyStackIndices = new();
        private CancellationTokenSource _processingCancellationToken = new();

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
                        StartProcessing();
                    }
                }

                if (Input.GetKeyState(Settings.EmergencyStopKey.Value.Key))
                {
                    ForceStopProcessing();
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

            // Find greed items
            _greedItems = inventoryItems
                .Where(x => x.Item.Path.Contains("DistilledEmotion3") && 
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

        private void StartProcessing()
        {
            if (_isProcessing) return;

            try
            {
                // Ensure cursor control by clicking off GUI if needed
                EnsureCursorControl();
                
                _isProcessing = true;
                _forceStop = false;
                _processingCancellationToken = new CancellationTokenSource();
                
                ProcessWaystones();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error starting processing: {ex.Message}", 10);
            }
            finally
            {
                _isProcessing = false;
                _forceStop = false;
            }
        }

        private void ForceStopProcessing()
        {
            _forceStop = true;
            _isProcessing = false;
            _processingCancellationToken?.Cancel();
            TaskRunner.StopAll();
            CurrencyOperations.CleanUp();
            DebugWindow.LogMsg("ThisIsTheWaystone: Processing force stopped and reset", 5);
        }

        private void EnsureCursorControl()
        {
            try
            {
                // Click somewhere safe to ensure cursor control
                var safePosition = new Vector2(100, 100);
                Input.SetCursorPos(safePosition);
                Thread.Sleep(100);
                Input.Click(MouseButtons.Left);
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error ensuring cursor control: {ex.Message}", 5);
            }
        }

        private bool ValidateItemSelection(NormalInventoryItem expectedItem, string operation)
        {
            try
            {
                // Check if the item still exists and has the expected properties
                if (expectedItem?.Item == null) return false;
                
                var baseComponent = expectedItem.Item.GetComponent<Base>();
                var stackComponent = expectedItem.Item.GetComponent<Stack>();
                
                if (baseComponent == null) return false;
                
                // For currency items, check if stack size is still valid
                if (stackComponent != null && stackComponent.Size <= 0) return false;
                
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error validating item for {operation}: {ex.Message}", 5);
                return false;
            }
        }

        private bool ValidateWorkflowStep(string stepName, bool condition)
        {
            if (_forceStop)
            {
                DebugWindow.LogMsg($"ThisIsTheWaystone: Workflow stopped at {stepName}", 5);
                return false;
            }

            if (!condition)
            {
                DebugWindow.LogError($"ThisIsTheWaystone: Workflow validation failed at {stepName}", 5);
                return false;
            }

            return true;
        }

        private NormalInventoryItem GetValidEmotionItem(List<NormalInventoryItem> emotionItems, string emotionName)
        {
            // Find the first emotion item with available stack size
            var validEmotion = emotionItems.FirstOrDefault(p => 
                p?.Item != null && 
                p.Item.GetComponent<Stack>()?.Size > 0);
            
            if (validEmotion == null)
            {
                DebugWindow.LogError($"No valid {emotionName} items found with available stack size", 5);
                return null;
            }

            // Double-check the item is still valid
            if (!ValidateItemSelection(validEmotion, $"{emotionName} Validation"))
            {
                DebugWindow.LogError($"Selected {emotionName} item failed validation", 5);
                return null;
            }

            return validEmotion;
        }

        private void ProcessWaystones()
        {
            try
            {
                if (!ValidateWorkflowStep("Initial Check", Settings.EnableWaystoneProcessing.Value))
                {
                    return;
                }

                // Check if stash is open - don't process if it is
                var stashPanel = GameController?.Game?.IngameState?.IngameUi?.StashElement;
                if (!ValidateWorkflowStep("Stash Check", stashPanel?.IsVisible != true))
                {
                    return; // Do nothing if stash is open
                }

                // Filter processable waystones using clean LINQ
                var processableWaystones = _waystones.Where(w => w.CanProcess && 
                    (w.NeedAugment || w.NeedAlchemy || w.NeedRegal || w.NeedExalt || w.NeedParanoia)).ToList();
                
                if (!ValidateWorkflowStep("Processable Waystones Check", processableWaystones.Any()))
                {
                    return;
                }

                // Process currency application
                if (!ValidateWorkflowStep("Currency Application", true))
                {
                    return;
                }
                ProcessCurrencyApplication(processableWaystones);

                // Process distilled emotions based on settings
                if (Settings.UseDistilledParanoia.Value && !Settings.UseDilutedLiquidGreed.Value && !Settings.UseNoDistilledEmotions.Value)
                {
                    if (ValidateWorkflowStep("Paranoia Application", true))
                    {
                        ProcessParanoiaApplication(processableWaystones);
                    }
                }
                else if (Settings.UseDilutedLiquidGreed.Value && !Settings.UseDistilledParanoia.Value && !Settings.UseNoDistilledEmotions.Value)
                {
                    if (ValidateWorkflowStep("Greed Application", true))
                    {
                        ProcessGreedApplication(processableWaystones);
                    }
                }

                // Cleanup
                if (ValidateWorkflowStep("Cleanup", true))
                {
                    PerformCleanup();
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error in ProcessWaystones: {ex.Message}", 5);
            }
        }

        private void ProcessCurrencyApplication(List<WaystoneData> waystones)
        {
            var waystonesNeedingCurrency = waystones.Where(w => w.NeedAugment || w.NeedAlchemy || w.NeedRegal || w.NeedExalt).ToList();
            
            if (!ValidateWorkflowStep("Currency Waystones Check", waystonesNeedingCurrency.Any())) return;

            foreach (var waystone in waystonesNeedingCurrency)
            {
                if (_forceStop) break;
                ProcessWaystoneCurrency(waystone);
            }
        }

        private void ProcessWaystoneCurrency(WaystoneData waystone)
        {
            try
            {
                if (_forceStop) return;

                // Apply Augmentation
                if (waystone.NeedAugment && ValidateWorkflowStep("Augmentation", true))
                {
                    ApplyCurrency("CurrencyAddModToMagic", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Alchemy
                if (waystone.NeedAlchemy && ValidateWorkflowStep("Alchemy", true))
                {
                    ApplyCurrency("CurrencyUpgradeToRare", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Regal
                if (waystone.NeedRegal && ValidateWorkflowStep("Regal", true))
                {
                    ApplyCurrency("CurrencyUpgradeMagicToRare", waystone, 1);
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Exalted Orbs (with real-time checking)
                if (waystone.NeedExalt && ValidateWorkflowStep("Exalted Orbs", true))
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
                if (_forceStop) break;

                // Check if waystone is already fully exalted before each exalt
                RecalculateWaystoneState(waystone);
                
                if (waystone.ModifierCount >= 6)
                {
                    break;
                }

                if (!ValidateWorkflowStep($"Exalt {i + 1}/{maxExalts}", true))
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
            
            if (!ValidateWorkflowStep("Paranoia Waystones Check", waystonesNeedingParanoia.Any())) return;

            // Process all waystones in a single distillation session
            ProcessAllWaystonesInDistillation(waystonesNeedingParanoia, _paranoiaItems, "Liquid Paranoia");
        }

        private void ProcessGreedApplication(List<WaystoneData> waystones)
        {
            var waystonesNeedingGreed = waystones.Where(w => w.NeedParanoia && !w.IsDistilled).ToList();
            
            if (!ValidateWorkflowStep("Greed Waystones Check", waystonesNeedingGreed.Any())) return;

            // Process all waystones in a single distillation session
            ProcessAllWaystonesInDistillation(waystonesNeedingGreed, _greedItems, "Diluted Liquid Greed");
        }

        private void ProcessAllWaystonesInDistillation(List<WaystoneData> waystones, List<NormalInventoryItem> emotionItems, string emotionName)
        {
            try
            {
                if (_forceStop) return;

                // Find emotion item with at least 3 charges
                var emotion = emotionItems.FirstOrDefault(p => p.Item.GetComponent<Stack>()?.Size >= 3);
                if (!ValidateWorkflowStep($"{emotionName} Availability", emotion != null))
                {
                    return;
                }

                // Validate emotion item before using
                if (!ValidateItemSelection(emotion, $"{emotionName} Selection"))
                {
                    DebugWindow.LogError($"Invalid {emotionName} item selected", 5);
                    return;
                }

                // Open distillation window once at the beginning
                CurrencyOperations.UseItemRightClick(emotion);
                Thread.Sleep(350);

                if (_forceStop) return;

                // Process each waystone in the distillation window (window remains open)
                foreach (var waystone in waystones)
                {
                    if (_forceStop) break;

                    // Validate waystone before processing
                    if (!ValidateItemSelection(waystone.InventoryItem, $"Waystone {waystone.Name}"))
                    {
                        DebugWindow.LogError($"Invalid waystone {waystone.Name} selected", 5);
                        continue;
                    }

                    // Step 3: Control + click the waystone for distillation
                    CurrencyOperations.CtrlClickItem(waystone.InventoryItem);
                    Thread.Sleep(200);

                    if (_forceStop) break;

                    // Step 4: Control + click the distillation materials 3 times (for each waystone)
                    for (int i = 0; i < 3; i++)
                    {
                        if (_forceStop) break;
                        
                        // Find a valid emotion item for this transfer (re-validate each time)
                        var currentEmotion = GetValidEmotionItem(emotionItems, emotionName);
                        if (currentEmotion == null)
                        {
                            DebugWindow.LogError($"No valid {emotionName} item available for transfer {i + 1}", 5);
                            break;
                        }

                        // Ensure we're clicking on the correct emotion item
                        CurrencyOperations.CtrlClickItem(currentEmotion);
                        Thread.Sleep(200);
                    }

                    if (_forceStop) break;

                    // Step 5: Click instill button (using configurable position)
                    var instillPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(Settings.InstillButtonX.Value, Settings.InstillButtonY.Value);
                    CurrencyOperations.ClickAtPosition(instillPos);
                    Thread.Sleep(750);

                    if (_forceStop) break;

                    // Step 6: Control + left click the waystone back into inventory
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
            if (!ValidateWorkflowStep($"Currency {currencyPath} Availability", currency != null))
            {
                return;
            }

            // Validate currency item before using
            if (!ValidateItemSelection(currency, $"Currency {currencyPath}"))
            {
                DebugWindow.LogError($"Invalid currency {currencyPath} selected", 5);
                return;
            }

            // Validate waystone before using
            if (!ValidateItemSelection(waystone.InventoryItem, $"Waystone {waystone.Name}"))
            {
                DebugWindow.LogError($"Invalid waystone {waystone.Name} selected", 5);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (_forceStop) break;

                if (!ValidateWorkflowStep($"Currency Application {i + 1}/{count}", true))
                {
                    break;
                }

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
                ImGui.Text($"Greed Items: {_greedItems.Count}");
                
                // Show currency counts with proper names
                ImGui.Separator();
                ImGui.Text("Currency Counts:");
                ShowCurrencyCount("Orb of Augmentation", "CurrencyAddModToMagic");
                ShowCurrencyCount("Orb of Alchemy", "CurrencyUpgradeToRare");
                ShowCurrencyCount("Regal Orb", "CurrencyUpgradeMagicToRare");
                ShowCurrencyCount("Exalted Orb", "CurrencyAddModToRare");
                
                // Distilled Emotion Options
                ImGui.Separator();
                ImGui.Text("Distilled Emotion Options:");
                
                // Use radio button behavior for mutually exclusive options
                bool useParanoia = Settings.UseDistilledParanoia.Value;
                bool useGreed = Settings.UseDilutedLiquidGreed.Value;
                bool useNone = Settings.UseNoDistilledEmotions.Value;
                
                if (ImGui.RadioButton("Use Paranoia", useParanoia))
                {
                    Settings.UseDistilledParanoia.Value = true;
                    Settings.UseDilutedLiquidGreed.Value = false;
                    Settings.UseNoDistilledEmotions.Value = false;
                }
                
                if (ImGui.RadioButton("Use Greed", useGreed))
                {
                    Settings.UseDistilledParanoia.Value = false;
                    Settings.UseDilutedLiquidGreed.Value = true;
                    Settings.UseNoDistilledEmotions.Value = false;
                }
                
                if (ImGui.RadioButton("Use None", useNone))
                {
                    Settings.UseDistilledParanoia.Value = false;
                    Settings.UseDilutedLiquidGreed.Value = false;
                    Settings.UseNoDistilledEmotions.Value = true;
                }
                
                ImGui.Separator();
                
                if (_isProcessing)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "PROCESSING...");
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
