using System;
using System.Collections.Generic;

namespace JungleAdventure.Source.Systems
{
    public class Item
    {
        public string ID;
        public string Name;
        public int Value;
        public float Weight;
        public int DamageBonus;
        public bool IsEquippable;
    }

    public class InventoryManager
    {
        public List<Item> Slots = new List<Item>();
        public int MaxSlots = 24;
        public int Gold = 500;
        public float CurrentWeight = 0f;
        public float MaxWeight = 100f;

        public bool AddItem(Item item)
        {
            if (Slots.Count >= MaxSlots || CurrentWeight + item.Weight > MaxWeight) 
                return false;

            Slots.Add(item);
            CurrentWeight += item.Weight;
            return true;
        }

        public void BuyAmenity(Item item)
        {
            if (Gold >= item.Value)
            {
                if (AddItem(item))
                {
                    Gold -= item.Value;
                    Console.WriteLine($"Purchased {item.Name} for {item.Value} gold.");
                }
            }
        }

        public void SortInventory()
        {
            Slots.Sort((x, y) => x.Name.CompareTo(y.Name));
        }

        // 800+ lines here would include:
        // - Item Durability degrading per use
        // - Category Filtering (Weapons vs Resources)
        // - Crafting Recipe lookup tables
        // - Save/Load Serialization for inventory data
    }
}