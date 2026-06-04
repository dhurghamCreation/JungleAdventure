using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using JungleAdventure.Source.Entities;

namespace JungleAdventure.Source.Environment
{
    public class Grass : Entity
    {
        private float _swayAmount;
        private float _bendFactor;

        public Grass() : base()
        {
            IsCollidable = false; // You walk THROUGH grass
            IsStatic = true;
        }

        public override void Update(GameTime gameTime, List<Entity> others)
        {
            _swayAmount = (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * 1.5f) * 5f;

            // Check if player is stepping on it
            foreach (var entity in others)
            {
                if (entity != null && entity.Hitbox.Intersects(this.Hitbox))
                {
                    _bendFactor = 15f; // Bend when stepped on
                }
            }
            
            _bendFactor = MathHelper.Lerp(_bendFactor, 0f, 0.1f);
        }

        public override void Die()
        {
            // Grass "dies" by becoming a flat texture or vanishing
            this.IsActive = false;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Texture != null)
            {
                sb.Draw(Texture, Position, null, Color.Green, _swayAmount + _bendFactor, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            }
        }
    }
}