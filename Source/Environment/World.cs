using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JungleAdventure.Source.Entities;

namespace JungleAdventure.Source.Environment
{
    public class World
    {
        public List<Entity> Entities { get; set; }

        public World()
        {
            Entities = new List<Entity>();
        }

        public void Update(GameTime gameTime)
        {
            // This is where jungle weather or day/night logic would go
        }
    }
}