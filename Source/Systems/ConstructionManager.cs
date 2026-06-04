using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using JungleAdventure.Source.Entities;
using JungleAdventure.Source.Environment;

namespace JungleAdventure.Source.Systems
{
    public class ConstructionManager
    {
        private const int TILE_SIZE = 64;
        public bool OverlayVisible = false;
        private string _activeBlueprint = "WoodenWall";
        
        // Dictionary for massive scalability of items
        private Dictionary<string, ConstructionData> _blueprints;

        public ConstructionManager()
        {
            _blueprints = new Dictionary<string, ConstructionData>
            {
                { "WoodenWall", new ConstructionData(15, 0, 50, "A sturdy barricade") },
                { "StoneFloor", new ConstructionData(0, 20, 100, "Clean stone walkway") },
                { "SmallHut", new ConstructionData(100, 50, 500, "A safe place to rest") },
                { "SwordForge", new ConstructionData(50, 100, 300, "Used to craft steel amenities") }
            };
        }

        public void Update(Player player, World world, KeyboardState k, MouseState m)
        {
            if (k.IsKeyDown(Keys.B)) OverlayVisible = true;
            if (k.IsKeyDown(Keys.Escape)) OverlayVisible = false;

            if (OverlayVisible)
            {
                Vector2 mouseGrid = GetGridSnapped(m.Position.ToVector2());
                
                if (m.LeftButton == ButtonState.Pressed)
                {
                    AttemptPlacement(player, world, mouseGrid);
                }
            }
        }

        private void AttemptPlacement(Player player, World world, Vector2 pos)
        {
            var data = _blueprints[_activeBlueprint];
            
            // Resource Check
            if (player.WoodCount < data.WoodCost || player.StoneCount < data.StoneCost) return;
            
            // Space Occupied Check
            foreach(var entity in world.Entities)
            {
                if (Vector2.Distance(entity.Position, pos) < TILE_SIZE / 2) return;
            }

            // Build it
            player.WoodCount -= data.WoodCost;
            player.StoneCount -= data.StoneCost;
            
            // logic to add actual object to world list
        }

        private Vector2 GetGridSnapped(Vector2 screenPos)
        {
            return new Vector2(
                (int)(screenPos.X / TILE_SIZE) * TILE_SIZE,
                (int)(screenPos.Y / TILE_SIZE) * TILE_SIZE
            );
        }
    }

    public struct ConstructionData
    {
        public int WoodCost;
        public int StoneCost;
        public int Health;
        public string Desc;

        public ConstructionData(int w, int s, int h, string d)
        {
            WoodCost = w; StoneCost = s; Health = h; Desc = d;
        }
    }
}