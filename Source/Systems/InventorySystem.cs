using System.Collections.Generic;

namespace JungleAdventure.Source.Systems
{
    public class InventorySystem
    {
        public struct ItemSlot
        {
            public string ItemName;
            public int Quantity;
        }

        public List<ItemSlot> Slots = new List<ItemSlot>();

        public void AddItem(string name, int amount)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].ItemName == name)
                {
                    var updated = Slots[i];
                    updated.Quantity += amount;
                    Slots[i] = updated;
                    return;
                }
            }
            Slots.Add(new ItemSlot { ItemName = name, Quantity = amount });
        }
    }
}