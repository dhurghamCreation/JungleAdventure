using System;
using Microsoft.Xna.Framework;
// ... other usingsusing Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace JungleAdventure.Source.Systems
{
    public class MapSystem
    {
        private const int CHUNK_SIZE = 1024; // Big world chunks
        private Dictionary<Vector2, ChunkData> _discoveredChunks = new Dictionary<Vector2, ChunkData>();
        
        public bool IsMapOpen = false;
        public float ZoomLevel = 1.0f;
        
        public void Update(Vector2 playerPos)
        {
            Vector2 currentChunk = new Vector2(
                (int)Math.Floor(playerPos.X / CHUNK_SIZE),
                (int)Math.Floor(playerPos.Y / CHUNK_SIZE)
            );

            if (!_discoveredChunks.ContainsKey(currentChunk))
            {
                _discoveredChunks.Add(currentChunk, new ChunkData { IsExplored = true, TimeFound = System.DateTime.Now });
            }
        }

        public void DrawMiniMap(SpriteBatch sb, Vector2 playerPos, Texture2D mapDot)
        {
            // Drawing logic for a circular minimap in the corner
            // 800+ lines would include:
            // - Scaling map coordinates to screen coordinates
            // - Rendering Waypoint Icons (Shops, Quest Givers)
            // - Drawing a "North" compass needle
            // - Fog of War texture masking
        }

        public void AddWaypoint(Vector2 pos, string label)
        {
            // System to store dynamic locations like "My First House"
        }
    }

    public struct ChunkData
    {
        public bool IsExplored;
        public System.DateTime TimeFound;
        public string BiomeType; // "DeepJungle", "DesertEdge", "AncientRuins"
    }
}