using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using JungleAdventure.Source.Entities;
using JungleAdventure.Source.Systems;
using JungleAdventure.Source.Core;

namespace JungleAdventure.Source.Entities.Enemies
{
    public class Zombie : Entity
    {
        private float _wanderTimer;
        public bool FacingRight = false;
        private float _verticalVelocity = 0f;

        public Zombie()
        {
            Name = "Zombie";
            Health = 60f;
            MaxHealth = 60f;
            BaseDamage = 12f;
            Speed = 90f;
            AttackCooldown = 1.1f;
            AIState = AIBehavior.Idle;
        }

        public override void Update(GameTime gameTime, List<Entity> others)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Player player = null;
            foreach (var entity in others)
            {
                if (entity is Player foundPlayer)
                {
                    player = foundPlayer;
                    break;
                }
            }

            float targetX = player != null ? player.Position.X : Position.X;
            float horizontalDirection = targetX >= Position.X ? 1f : -1f;
            FacingRight = horizontalDirection >= 0f;

            if (AIState == AIBehavior.Idle && player != null)
            {
                float distanceToPlayer = Math.Abs(player.Position.X - Position.X);
                if (distanceToPlayer < 900f)
                {
                    AIState = AIBehavior.Chase;
                }
            }

            if (AIState == AIBehavior.Chase && player != null)
            {
                float chaseSpeed = Speed * 0.95f;
                Position.X += horizontalDirection * chaseSpeed * dt;
                Velocity = new Vector2(horizontalDirection * chaseSpeed, 0f);
            }
            else
            {
                _wanderTimer += dt;
                Position.X += (float)Math.Sin(_wanderTimer * 0.7f) * 12f * dt;
                Velocity = new Vector2((float)Math.Sin(_wanderTimer * 0.7f) * 12f, 0f);
            }

            if (Position.Y < Globals.GroundY)
            {
                _verticalVelocity += 2000f * dt;
                Position.Y += _verticalVelocity * dt;
            }

            if (Position.Y >= Globals.GroundY)
            {
                Position.Y = Globals.GroundY;
                _verticalVelocity = 0f;
            }

            Hitbox = new Rectangle((int)Position.X - 16, (int)Position.Y - 54, 32, 54);

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
                int x = (int)Position.X;
                int y = (int)Position.Y;
                int dir = FacingRight ? 1 : -1;

                // Shadow
                sb.Draw(Texture, new Rectangle(x - 18, y + 4, 36, 6), Color.Black * 0.25f);

                // Legs
                sb.Draw(Texture, new Rectangle(x - 9, y - 18, 8, 18), new Color(35, 70, 38));
                sb.Draw(Texture, new Rectangle(x + 1, y - 18, 8, 18), new Color(42, 82, 46));

                // Body
                sb.Draw(Texture, new Rectangle(x - 11, y - 42, 22, 26), new Color(70, 118, 70));

                // Arms, bent forward in side view
                sb.Draw(Texture, new Rectangle(x - 14 * dir, y - 36, 7, 18), new Color(60, 105, 60));
                sb.Draw(Texture, new Rectangle(x + 9 * dir, y - 34, 7, 16), new Color(60, 105, 60));

                // Head
                sb.Draw(Texture, new Rectangle(x - 9, y - 58, 18, 18), new Color(132, 166, 88));
                sb.Draw(Texture, new Rectangle(x - 10, y - 61, 20, 7), new Color(68, 50, 38));

                // Face details
                sb.Draw(Texture, new Rectangle(x + (FacingRight ? 1 : -1), y - 53, 4, 4), Color.Black);
                sb.Draw(Texture, new Rectangle(x + (FacingRight ? 8 : -8), y - 51, 3, 2), Color.DarkRed);
                sb.Draw(Texture, new Rectangle(x + (FacingRight ? 4 : -4), y - 46, 6, 2), new Color(90, 35, 35));
            }
        }
    }
}
