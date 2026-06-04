using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using JungleAdventure.Source.Entities;

namespace JungleAdventure.Source.Entities.NPCs
{
    public class Wizard : Entity
    {
        private float _idleTimer;
        private float _floatOffset;

        public Wizard() : base()
        {
            Health = 200f;
            MaxHealth = 200f;
            IsStatic = true;
            _idleTimer = 0;
        }

        public override void Update(GameTime gameTime, List<Entity> others)
        {
            _idleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _floatOffset = (float)System.Math.Sin(_idleTimer * 2f) * 3f;
            
            base.Update(gameTime, others);
        }

        public override void OnInteract(Player player)
        {
            // Massive dialogue logic starts here...
        }

        public override void Die()
        {
            // Wizards are immortal - just reset
            Health = MaxHealth;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Texture != null)
            {
                // Draw wizard as a purple entity with floating effect
                float drawY = Position.Y + _floatOffset;
                sb.Draw(Texture, new Rectangle((int)Position.X - 16, (int)drawY - 16, 32, 32), Color.Purple);
                
                // Draw hat (small triangle on top)
                sb.Draw(Texture, new Rectangle((int)Position.X - 8, (int)drawY - 24, 16, 8), Color.DarkViolet);
                
                // Draw "name" indicator - small white dot
                sb.Draw(Texture, new Rectangle((int)Position.X - 2, (int)drawY - 30, 4, 4), Color.Yellow);
            }
            else
            {
                // Fallback
                if (sb.GraphicsDevice != null)
                {
                    Texture2D pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                    pixel.SetData(new[] { Color.Purple });
                    sb.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32), Color.White);
                }
            }
        }
    }
}