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
        /// Apply currency to target item using right-click + left-click pattern
        /// </summary>
        public static bool UseCurrencyOnItem(NormalInventoryItem currency, NormalInventoryItem target, string currencyPath)
        {
            try
            {
                // Move cursor to currency and right-click
                var currencyPos = currency.GetClientRect().Center;
                Input.SetCursorPos(currencyPos);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Right);
                Thread.Sleep(110);

                // Move cursor to target and left-click
                var targetPos = target.GetClientRect().Center;
                Input.SetCursorPos(targetPos);
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
        public static bool CtrlClickItem(NormalInventoryItem item, string itemType, string operation)
        {
            try
            {
                // Move cursor to item and Ctrl+click
                var itemPos = item.GetClientRect().Center;
                Input.SetCursorPos(itemPos);
                Thread.Sleep(80);
                
                // Hold Ctrl and click
                Input.KeyDown(Keys.ControlKey);
                Input.Click(MouseButtons.Left);
                Input.KeyUp(Keys.ControlKey);
                Thread.Sleep(200);
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error Ctrl+clicking {itemType}: {ex.Message}", 5);
                return false;
            }
        }

        /// <summary>
        /// Right-click an item (for using emotions)
        /// </summary>
        public static bool UseItemRightClick(NormalInventoryItem item, string itemType, string operation)
        {
            try
            {
                // Move cursor to item and right-click
                var itemPos = item.GetClientRect().Center;
                Input.SetCursorPos(itemPos);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Right);
                Thread.Sleep(200);
                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error right-clicking {itemType}: {ex.Message}", 5);
                return false;
            }
        }

        /// <summary>
        /// Click at a specific position
        /// </summary>
        public static void ClickAtPosition(Vector2 position)
        {
            try
            {
                Input.SetCursorPos(position);
                Thread.Sleep(50);
                Input.Click(MouseButtons.Left);
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error clicking at position: {ex.Message}", 5);
            }
        }

        /// <summary>
        /// Press a key
        /// </summary>
        public static void PressKey(Keys key)
        {
            try
            {
                Input.KeyDown(key);
                Thread.Sleep(50);
                Input.KeyUp(key);
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error pressing key: {ex.Message}", 5);
            }
        }

        /// <summary>
        /// Clean up any ongoing operations
        /// </summary>
        public static void CleanUp()
        {
            try
            {
                // Press Escape to close any open windows
                PressKey(Keys.Escape);
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error during cleanup: {ex.Message}", 5);
            }
        }
    }
}