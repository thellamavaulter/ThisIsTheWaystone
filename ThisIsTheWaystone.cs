using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
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
    /// <summary>
    /// Represents an emotion item (paranoia/greed) with stack tracking
    /// Based on the DistilItem pattern from map-crafter plugin
    /// </summary>
    public class EmotionItem
    {
        public NormalInventoryItem InventoryItem { get; }
        public int StackSize { get; set; }
        public Vector2 ClickPos { get; }
        public string EmotionName { get; }

        public EmotionItem(NormalInventoryItem inventoryItem, string emotionName)
        {
            InventoryItem = inventoryItem;
            EmotionName = emotionName;
            ClickPos = inventoryItem.GetClientRect().Center;
            
            // Initialize stack size from the item's current stack
            try
            {
                var stackComponent = inventoryItem.Item.GetComponent<Stack>();
                StackSize = stackComponent?.Size ?? 0;
            }
            catch
            {
                StackSize = 0;
            }
        }

        /// <summary>
        /// Decrements the stack size and returns true if successful
        /// </summary>
        public bool UseItem()
        {
            if (StackSize > 0)
            {
                StackSize--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if this item has available stack
        /// </summary>
        public bool HasAvailableStack => StackSize > 0;
    }
   
    public class ThisIsTheWaystone : BaseSettingsPlugin<ThisIsTheWaystoneSettings>
    {
        private volatile bool _isProcessing = false;
        private volatile bool _forceStop = false;
        private List<WaystoneData> _waystones = new();
        private List<EmotionItem> _paranoiaItems = new();
        private List<EmotionItem> _greedItems = new();
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
                .Select(x => new EmotionItem(x, "Liquid Paranoia"))
                .ToList();

            // Find greed items
            _greedItems = inventoryItems
                .Where(x => x.Item.Path.Contains("DistilledEmotion3") && 
                           x.Item.GetComponent<Stack>()?.Size > 0)
                .Select(x => new EmotionItem(x, "Diluted Liquid Greed"))
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
            
            // Reset the force stop flag after a brief delay to allow for cleanup
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                _forceStop = false;
            });
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




        private EmotionItem GetValidEmotionItem(List<EmotionItem> emotionItems, string emotionName)
        {
            try
            {
                // Find the first emotion item with available stack size using map-crafter pattern
                var validEmotion = emotionItems.FirstOrDefault(x => x.HasAvailableStack);
                
                if (validEmotion == null)
                {
                    DebugWindow.LogError($"No valid {emotionName} items found with available stack size", 5);
                    return null;
                }

                // Log which stack we're using for debugging
                DebugWindow.LogMsg($"Using {emotionName} stack with {validEmotion.StackSize} remaining", 3);

                return validEmotion;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error getting valid {emotionName} item: {ex.Message}", 5);
                return null;
            }
        }

        /// <summary>
        /// Refresh the emotion items list to get current stack information
        /// Based on the ParseDistilItems pattern from map-crafter plugin
        /// </summary>
        private void RefreshEmotionItems(List<EmotionItem> emotionItems, string emotionName)
        {
            try
            {
                emotionItems.Clear();
                var inventoryItems = GameController?.Game?.IngameState?.IngameUi?.InventoryPanel?[InventoryIndex.PlayerInventory]?.VisibleInventoryItems;
                
                if (inventoryItems != null)
                {
                    foreach (var item in inventoryItems)
                    {
                        try
                        {
                            var baseComponent = item.Item.GetComponent<Base>();
                            if (baseComponent != null && baseComponent.Name.Contains(emotionName))
                            {
                                var emotionItem = new EmotionItem(item, emotionName);
                                if (emotionItem.StackSize > 0) // Only add items with available stack
                                {
                                    emotionItems.Add(emotionItem);
                                }
                            }
                        }
                        catch
                        {
                            // Skip invalid items
                        }
                    }
                }
                
                DebugWindow.LogMsg($"Refreshed {emotionName} items list: {emotionItems.Count} items found with total stack size {emotionItems.Sum(x => x.StackSize)}", 3);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error refreshing {emotionName} items: {ex.Message}", 5);
            }
        }


        /// <summary>
        /// Get the item currently being hovered over by the cursor
        /// </summary>
        private NormalInventoryItem GetItemAtCursor()
        {
            try
            {
                var cursorPos = Input.MousePosition;
                var inventoryItems = GameController?.Game?.IngameState?.IngameUi?.InventoryPanel?[InventoryIndex.PlayerInventory]?.VisibleInventoryItems;
                
                if (inventoryItems == null) return null;
                
                return inventoryItems.FirstOrDefault(item => 
                {
                    try
                    {
                        var rect = item.GetClientRect();
                        return rect.Contains(cursorPos);
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error getting item at cursor: {ex.Message}", 5);
                return null;
            }
        }

        /// <summary>
        /// Get the display name of an item for GUI display
        /// </summary>
        private string GetItemDisplayName(NormalInventoryItem item)
        {
            try
            {
                if (item?.Item == null) return "Unknown Item";
                
                var baseComponent = item.Item.GetComponent<Base>();
                if (baseComponent != null)
                {
                    return baseComponent.Name ?? "Unknown Item";
                }
                
                return "Unknown Item";
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error getting item display name: {ex.Message}", 5);
                return "Error";
            }
        }




        private void ProcessWaystones()
        {
            try
            {
                if (_forceStop) return;
                
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
                if (_forceStop) return;
                ProcessCurrencyApplication(processableWaystones);

                // Process distilled emotions based on settings
                if (_forceStop) return;
                if (Settings.UseDistilledParanoia.Value && !Settings.UseDilutedLiquidGreed.Value && !Settings.UseNoDistilledEmotions.Value)
                {
                    ProcessParanoiaApplication(processableWaystones);
                }
                else if (Settings.UseDilutedLiquidGreed.Value && !Settings.UseDistilledParanoia.Value && !Settings.UseNoDistilledEmotions.Value)
                {
                    ProcessGreedApplication(processableWaystones);
                }

                // Cleanup
                if (_forceStop) return;
                PerformCleanup();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error in ProcessWaystones: {ex.Message}", 5);
            }
        }

        private void ProcessCurrencyApplication(List<WaystoneData> waystones)
        {
            if (_forceStop) return;
            
            var waystonesNeedingCurrency = waystones.Where(w => w.NeedAugment || w.NeedAlchemy || w.NeedRegal || w.NeedExalt).ToList();
            
            if (!waystonesNeedingCurrency.Any()) return;

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
                if (waystone.NeedAugment)
                {
                    if (_forceStop) return;
                    ApplyCurrency("CurrencyAddModToMagic", waystone, 1);
                    if (_forceStop) return;
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Alchemy
                if (waystone.NeedAlchemy)
                {
                    if (_forceStop) return;
                    ApplyCurrency("CurrencyUpgradeToRare", waystone, 1);
                    if (_forceStop) return;
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Regal
                if (waystone.NeedRegal)
                {
                    if (_forceStop) return;
                    ApplyCurrency("CurrencyUpgradeMagicToRare", waystone, 1);
                    if (_forceStop) return;
                    RecalculateWaystoneState(waystone);
                }

                if (_forceStop) return;

                // Apply Exalted Orbs (with real-time checking)
                if (waystone.NeedExalt)
                {
                    if (_forceStop) return;
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
            if (_forceStop) return;
            
            int exaltsApplied = 0;
            int maxExalts = waystone.ExaltLeft;

            for (int i = 0; i < maxExalts; i++)
            {
                if (_forceStop) break;

                // Check if waystone is already fully exalted before each exalt
                if (_forceStop) return;
                RecalculateWaystoneState(waystone);
                
                if (waystone.ModifierCount >= 6)
                {
                    break;
                }

                if (_forceStop) break;
                ApplyCurrency("CurrencyAddModToRare", waystone, 1);
                exaltsApplied++;
                
                if (_forceStop) break;
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
            if (_forceStop) return;
            
            var waystonesNeedingParanoia = waystones.Where(w => w.NeedParanoia && !w.IsDistilled).ToList();
            
            if (!waystonesNeedingParanoia.Any()) return;

            // Process all waystones in a single distillation session
            if (_forceStop) return;
            ProcessAllWaystonesInDistillation(waystonesNeedingParanoia, _paranoiaItems, "Liquid Paranoia");
        }

        private void ProcessGreedApplication(List<WaystoneData> waystones)
        {
            if (_forceStop) return;
            
            var waystonesNeedingGreed = waystones.Where(w => w.NeedParanoia && !w.IsDistilled).ToList();
            
            if (!waystonesNeedingGreed.Any()) return;

            // Process all waystones in a single distillation session
            if (_forceStop) return;
            ProcessAllWaystonesInDistillation(waystonesNeedingGreed, _greedItems, "Diluted Liquid Greed");
        }

        private void ProcessAllWaystonesInDistillation(List<WaystoneData> waystones, List<EmotionItem> emotionItems, string emotionName)
        {
            try
            {
                if (_forceStop) return;

                // Refresh emotion items and calculate total available
                RefreshEmotionItems(emotionItems, emotionName);
                var totalEmotionLeft = emotionItems.Sum(item => item.StackSize);
                
                if (totalEmotionLeft < 3)
                {
                    DebugWindow.LogError($"Insufficient {emotionName} items available. Need at least 3, have {totalEmotionLeft}", 5);
                    return;
                }

                DebugWindow.LogMsg($"Starting distillation with {totalEmotionLeft} {emotionName} items available", 3);

                // Process each waystone in the distillation window
                foreach (var waystone in waystones)
                {
                    if (_forceStop) break;

                    DebugWindow.LogMsg($"Processing waystone: {waystone.Name}", 3);

                    // Check if we have enough emotion items for this waystone (need 3)
                    if (totalEmotionLeft < 3)
                    {
                        DebugWindow.LogError($"Insufficient {emotionName} items for waystone {waystone.Name}. Need 3, have {totalEmotionLeft}", 5);
                        break;
                    }

                    // Step 1: Right-click the liquid (greed or paranoia) to open distillation window
                    if (_forceStop) break;
                    var currentLiquid = GetValidEmotionItem(emotionItems, emotionName);
                    if (currentLiquid == null)
                    {
                        DebugWindow.LogError($"No valid {emotionName} item available to start distillation", 5);
                        continue;
                    }

                    // Right-click the liquid to open distillation window
                    Input.SetCursorPos(currentLiquid.ClickPos);
                    if (_forceStop) break;
                    Thread.Sleep(100);
                    Input.Click(MouseButtons.Right);
                    if (_forceStop) break;
                    Thread.Sleep(500); // Wait for distillation window to open

                    // Step 2: Control + click the waystone into the distillation window
                    if (_forceStop) break;
                    if (!CurrencyOperations.CtrlClickItem(waystone.InventoryItem, "waystone", $"Waystone {waystone.Name} Transfer"))
                    {
                        DebugWindow.LogError($"Failed to transfer waystone {waystone.Name} to distillation window", 5);
                        continue;
                    }
                    if (_forceStop) break;
                    Thread.Sleep(200);

                    // Step 3: Control + click the liquid stack 3 times with dynamic stack management
                    for (int i = 0; i < 3; i++)
                    {
                        if (_forceStop) break;
                        
                        // Find a valid emotion item for this transfer using map-crafter pattern
                        var currentEmotion = emotionItems.FirstOrDefault(x => x.HasAvailableStack);
                        if (currentEmotion == null)
                        {
                            DebugWindow.LogError($"No valid {emotionName} item available for transfer {i + 1}", 5);
                            break;
                        }

                        // Use the emotion item and decrement its stack
                        if (!currentEmotion.UseItem())
                        {
                            DebugWindow.LogError($"Failed to use {emotionName} item for transfer {i + 1}", 5);
                            break;
                        }
                        
                        totalEmotionLeft--; // Decrement global counter

                        // Control + click the liquid into distillation window
                        if (_forceStop) break;
                        if (!CurrencyOperations.CtrlClickItem(currentEmotion.InventoryItem, "emotion", $"Liquid Transfer {i + 1}"))
                        {
                            DebugWindow.LogError($"Failed to transfer liquid {i + 1} to distillation window", 5);
                            break;
                        }
                        if (_forceStop) break;
                        Thread.Sleep(200);
                    }

                    if (_forceStop) break;

                    // Step 4: Click instill button
                    if (_forceStop) break;
                    var instillPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(Settings.InstillButtonX.Value, Settings.InstillButtonY.Value);
                    CurrencyOperations.ClickAtPosition(instillPos);
                    if (_forceStop) break;
                    Thread.Sleep(750);

                    if (_forceStop) break;

                    // Step 5: Control + click the waystone back into inventory
                    if (_forceStop) break;
                    var waystoneInDistillationPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(Settings.DistillWaystoneX.Value, Settings.DistillWaystoneY.Value);
                    Input.SetCursorPos(waystoneInDistillationPos);
                    if (_forceStop) break;
                    Thread.Sleep(100);
                    Input.KeyDown(Keys.ControlKey);
                    if (_forceStop) break;
                    Thread.Sleep(50);
                    Input.Click(MouseButtons.Left);
                    if (_forceStop) break;
                    Thread.Sleep(60);
                    Input.KeyUp(Keys.ControlKey);
                    if (_forceStop) break;
                    Thread.Sleep(350);

                    DebugWindow.LogMsg($"Completed processing waystone: {waystone.Name}. {totalEmotionLeft} {emotionName} items remaining", 3);
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
            if (_forceStop) return;
            
            var currency = GetCurrencyItem(currencyPath);
            if (currency == null) return;

            for (int i = 0; i < count; i++)
            {
                if (_forceStop) break;

                CurrencyOperations.UseCurrencyOnItem(currency, waystone.InventoryItem, currencyPath);
                
                if (_forceStop) break;
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
                if (_forceStop) return;
                
                // Press Escape to close any open windows
                CurrencyOperations.PressKey(Keys.Escape);
                if (_forceStop) return;
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
                ImGui.Text($"Paranoia Items: {_paranoiaItems.Count} (Total: {_paranoiaItems.Sum(x => x.StackSize)})");
                ImGui.Text($"Greed Items: {_greedItems.Count} (Total: {_greedItems.Sum(x => x.StackSize)})");
                
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

                // Show hovered item information
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1), "Hovered Item:");
                
                var hoveredItem = GetItemAtCursor();
                if (hoveredItem != null)
                {
                    var itemName = GetItemDisplayName(hoveredItem);
                    ImGui.TextColored(new Vector4(1f, 1f, 0f, 1), itemName);
                    
                    // Show additional item info if available
                    try
                    {
                        var stackComponent = hoveredItem.Item.GetComponent<Stack>();
                        if (stackComponent != null && stackComponent.Size > 1)
                        {
                            ImGui.Text($"Stack Size: {stackComponent.Size}");
                        }
                        
                        var modsComponent = hoveredItem.Item.GetComponent<Mods>();
                        if (modsComponent != null)
                        {
                            ImGui.Text($"Rarity: {modsComponent.ItemRarity}");
                            ImGui.Text($"Modifiers: {modsComponent.ItemMods?.Count ?? 0}");
                        }
                    }
                    catch
                    {
                        // Ignore errors getting additional info
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "No item under cursor");
                }
                
                ImGui.End();
            }
        }
    }
}
