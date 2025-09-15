using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Enums;
using Vector2 = System.Numerics.Vector2;

namespace ThisIsTheWaystone.UtilityClasses
{
    public static class CurrencyOperations
    {
        /// <summary>
        /// Find a currency item with available stack size
        /// </summary>
        public static NormalInventoryItem GetCurrencyItem(string currencyPath, ExileCore2.GameController gameController)
        {
            var inventoryItems = gameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            return inventoryItems
                .Where(x => x.Item.Path.Contains(currencyPath) && x.Item.HasComponent<Stack>())
                .FirstOrDefault(x => x.Item.GetComponent<Stack>().Size > 0);
        }

        /// <summary>
        /// Apply currency to target item using right-click + left-click pattern
        /// </summary>
        public static bool UseCurrencyOnItem(NormalInventoryItem currency, NormalInventoryItem target)
        {
            try
            {
                Input.SetCursorPos(currency.GetClientRect().Center);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Right);
                Thread.Sleep(110);

                Input.SetCursorPos(target.GetClientRect().Center);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Left);
                Thread.Sleep(200);
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error using currency on item: {ex.Message}", 5);
                return false;
            }
        }

        /// <summary>
        /// Ctrl+Click an item (for transferring to distillation window)
        /// </summary>
        public static void CtrlClickItem(NormalInventoryItem item)
        {
            Input.KeyDown(Keys.ControlKey);
            Thread.Sleep(50);
            Input.SetCursorPos(item.GetClientRect().Center);
            Thread.Sleep(80);
            Input.Click(MouseButtons.Left);
            Thread.Sleep(60);
            Input.KeyUp(Keys.ControlKey);
            Thread.Sleep(50);
        }

        /// <summary>
        /// Right-click an item (for opening distillation window)
        /// </summary>
        public static void UseItemRightClick(NormalInventoryItem item)
        {
            Input.SetCursorPos(item.GetClientRect().Center);
            Thread.Sleep(80);
            Input.Click(MouseButtons.Right);
            Thread.Sleep(80);
        }

        /// <summary>
        /// Click at a specific position with proper delays
        /// </summary>
        public static void ClickAtPosition(Vector2 position, int delayMs = 100)
        {
            Input.SetCursorPos(position);
            Thread.Sleep(delayMs);
            Input.Click(MouseButtons.Left);
            Thread.Sleep(delayMs);
        }

        /// <summary>
        /// Press a key with proper timing
        /// </summary>
        public static void PressKey(Keys key, int repetitions = 1)
        {
            for (int i = 0; i < repetitions; i++)
            {
                Input.KeyDown(key);
                Thread.Sleep(10);
                Input.KeyUp(key);
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Clean up any held keys
        /// </summary>
        public static void CleanUp()
        {
            Input.KeyUp(Keys.LControlKey);
            Input.KeyUp(Keys.Shift);
            Input.KeyUp(Keys.ControlKey);
        }
    }
}
