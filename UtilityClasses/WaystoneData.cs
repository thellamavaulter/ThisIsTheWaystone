using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExileCore2.Shared.Enums;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.Components;

namespace ThisIsTheWaystone.UtilityClasses
{
    /// <summary>
    /// Clean data structure for waystone processing state
    /// Based on MapCrafter's WaystoneItem pattern
    /// </summary>
    public class WaystoneData
    {
        public NormalInventoryItem InventoryItem { get; }
        public Vector2 ClickPos { get; }
        public string Name { get; }
        public ItemRarity Rarity { get; set; }
        public int ModifierCount { get; set; }
        public bool IsDistilled { get; set; }
        
        // Currency needs
        public bool NeedAugment { get; set; } = false;
        public bool NeedAlchemy { get; set; } = false;
        public bool NeedRegal { get; set; } = false;
        public bool NeedExalt { get; set; } = false;
        public int ExaltLeft { get; set; } = 0;
        
        // Paranoia needs
        public bool NeedParanoia { get; set; } = false;
        public int ParanoiaLeft { get; set; } = 0;
        
        // Processing state
        public bool CanProcess { get; set; } = true;
        public string Status { get; set; } = "Ready";

        public WaystoneData(NormalInventoryItem inventoryItem, Vector2 clickPos)
        {
            InventoryItem = inventoryItem;
            ClickPos = clickPos;
            
            // Extract properties from inventory item
            var baseComponent = inventoryItem.Item.GetComponent<Base>();
            var modsComponent = inventoryItem.Item.GetComponent<Mods>();
            
            Name = baseComponent?.Name ?? "Unknown";
            Rarity = modsComponent?.ItemRarity ?? ItemRarity.Normal;
            ModifierCount = modsComponent?.ItemMods?.Count ?? 0;
            IsDistilled = modsComponent?.ItemMods?.Any(m => m.DisplayName.Contains("InstilledMapDelirium")) ?? false;
        }

        /// <summary>
        /// Calculate what this waystone needs based on its current state
        /// </summary>
        public void CalculateNeeds()
        {
            var enchantedCount = ModifierCount - (IsDistilled ? 3 : 0); // Subtract distilled mods from count
            
            switch (Rarity)
            {
                case ItemRarity.Normal:
                    if (ModifierCount == 0)
                    {
                        NeedAlchemy = true;
                        NeedExalt = true;
                        ExaltLeft = 3; // Alchemy can add 3-6 mods, worst case needs 3 exalts
                    }
                    break;
                    
                case ItemRarity.Magic:
                    if (ModifierCount == 1)
                    {
                        NeedAugment = true;
                        NeedRegal = true;
                        NeedExalt = true;
                        ExaltLeft = 3;
                    }
                    else if (ModifierCount == 2)
                    {
                        NeedRegal = true;
                        NeedExalt = true;
                        ExaltLeft = 3;
                    }
                    break;
                    
                case ItemRarity.Rare:
                    if (ModifierCount >= 3 && ModifierCount < 6)
                    {
                        NeedExalt = true;
                        ExaltLeft = 6 - ModifierCount;
                    }
                    else if (ModifierCount == 6)
                    {
                        // Fully exalted, only needs paranoia
                        NeedParanoia = true;
                        ParanoiaLeft = 3;
                    }
                    break;
            }
            
            // All waystones that need currency will also need paranoia after currency application
            // UNLESS they are already distilled
            if ((NeedAugment || NeedAlchemy || NeedRegal || NeedExalt) && !IsDistilled)
            {
                NeedParanoia = true;
                ParanoiaLeft = 3;
            }
        }

        /// <summary>
        /// Get a summary of what this waystone needs
        /// </summary>
        public string GetNeedsSummary()
        {
            var needs = new List<string>();
            
            if (NeedAugment) needs.Add("Augment");
            if (NeedAlchemy) needs.Add("Alchemy");
            if (NeedRegal) needs.Add("Regal");
            if (NeedExalt) needs.Add($"{ExaltLeft} Exalt(s)");
            if (NeedParanoia) needs.Add($"{ParanoiaLeft} Paranoia");
            
            return needs.Count > 0 ? string.Join(", ", needs) : "Nothing";
        }
    }
}
