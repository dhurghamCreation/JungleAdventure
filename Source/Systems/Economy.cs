using System.Collections.Generic;

namespace JungleAdventure.Source.Systems
{
    public class Inventory
    {
        public int Gold = 100;
        public List<string> Items = new List<string>(); // Use 'Items' instead of 'Weapons'

        public bool BuySword()
        {
            if (Gold < 50)
            {
                return false;
            }

            Gold -= 50;
            Items.Add("Iron Sword");
            return true;
        }

        public bool BuyHealthPotion()
        {
            if (Gold < 25)
            {
                return false;
            }

            Gold -= 25;
            Items.Add("Health Potion");
            return true;
        }
    }
}