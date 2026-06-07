using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JungleAdventure.Source.Entities;
using JungleAdventure.Source.Systems;

namespace JungleAdventure.Source.Systems
{
    public class BuildingSystem
    {
        public void PlaceStructure(Vector2 mousePos, string structureType)
        {
            // Snap the mouse position to a 32x32 grid
            float x = (float)System.Math.Floor(mousePos.X / 32) * 32;
            float y = (float)System.Math.Floor(mousePos.Y / 32) * 32;
            
          
        }
    }
}