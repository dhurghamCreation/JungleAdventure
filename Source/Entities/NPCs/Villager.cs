using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace JungleAdventure.Source.Entities.NPCs
{
    public class Villager : Entity
    {
        private float _idleTimer;
        private float _walkTimer;
        private Vector2 _walkTarget;

        public Villager() : base()
        {
            Health = 50f;
            MaxHealth = 50f;
            IsStatic = false;
            Speed = 40f;
            _walkTarget = Position;
        }

        public override void Update(GameTime gameTime, List<Entity> others)
        {
            _idleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _walkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Wander around slowly
            if (_walkTimer > 3f)
            {
                _walkTarget = new Vector2(
                    Position.X + (float)(new System.Random().NextDouble() * 100f - 50f),
                    Position.Y + (float)(new System.Random().NextDouble() * 100f - 50f)
                );
                _walkTimer = 0;
            }

            Vector2 dir = _walkTarget - Position;
            if (dir.Length() > 5f)
            {
                dir.Normalize();
                Position += dir * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            Hitbox = new Rectangle((int)Position.X - 14, (int)Position.Y - 14, 28, 28);
            base.Update(gameTime, others);
        }

        public override void Die()
        {
            IsActive = false;
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Texture != null)
            {
                // Draw villager as orange entity
                sb.Draw(Texture, new Rectangle((int)Position.X - 14, (int)Position.Y - 14, 28, 28), Color.Orange);
                // Draw a small "hat"
                sb.Draw(Texture, new Rectangle((int)Position.X - 8, (int)Position.Y - 20, 16, 6), Color.Brown);
            }
            else
            {
                sb.Draw(Texture ?? new Texture2D(sb.GraphicsDevice, 1, 1),
                    new Rectangle((int)Position.X - 14, (int)Position.Y - 14, 28, 28), Color.Orange);
            }
        }
    }
}