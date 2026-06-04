using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using JungleAdventure.Source.Core;

namespace JungleAdventure.Source.Entities
{
    public class Player : Entity
    {
        public int WoodCount = 0;
        public int StoneCount = 0;
        public List<string> Inventory = new List<string>();
        public bool FacingRight = true;
        public bool IsOnGround = true;
        public bool IsSwinging = false;

        private float _verticalVelocity = 0f;
        private float _attackTimer = 0f;

        private const float JumpVelocity = -760f;
        private const float Gravity = 2200f;

        public Player() : base()
        {
            Speed = 250f;
            Health = 100f;
            MaxHealth = 100f;
        }

        public override void Update(GameTime gameTime, List<Entity> others)
        {
            var kstate = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveX = 0f;

            if (kstate.IsKeyDown(Keys.A)) moveX -= 1f;
            if (kstate.IsKeyDown(Keys.D)) moveX += 1f;

            if (moveX > 0f)
            {
                FacingRight = true;
            }
            else if (moveX < 0f)
            {
                FacingRight = false;
            }

            if ((kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Up)) && IsOnGround)
            {
                _verticalVelocity = JumpVelocity;
                IsOnGround = false;
            }

            Position.X += moveX * Speed * dt;
            _verticalVelocity += Gravity * dt;
            Position.Y += _verticalVelocity * dt;

            if (Position.Y >= Globals.GroundY)
            {
                Position.Y = Globals.GroundY;
                _verticalVelocity = 0f;
                IsOnGround = true;
            }

            if (_attackTimer > 0f)
            {
                _attackTimer -= dt;
                if (_attackTimer <= 0f)
                {
                    IsSwinging = false;
                }
            }

            Velocity = new Vector2(moveX * Speed, _verticalVelocity);
            
            // Update hitbox around the torso, not a cube sprite
            Hitbox = new Rectangle((int)Position.X - 14, (int)Position.Y - 54, 28, 54);

            base.Update(gameTime, others);
        }

        public void TriggerAttackAnimation()
        {
            IsSwinging = true;
            _attackTimer = 0.22f;
        }

        public override void Die()
        {
            // Reset player or show Game Over screen logic
            Position = new Vector2(160, Globals.GroundY);
            Health = MaxHealth;
            _verticalVelocity = 0f;
            IsOnGround = true;
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
                sb.Draw(Texture, new Rectangle(x - 8, y - 18, 7, 18), new Color(58, 66, 96));
                sb.Draw(Texture, new Rectangle(x + 1, y - 18, 7, 18), new Color(48, 58, 82));

                // Boots
                sb.Draw(Texture, new Rectangle(x - 10, y - 2, 10, 5), new Color(34, 28, 22));
                sb.Draw(Texture, new Rectangle(x + 2, y - 2, 10, 5), new Color(34, 28, 22));

                // Torso
                sb.Draw(Texture, new Rectangle(x - 10, y - 42, 20, 24), new Color(44, 96, 166));

                // Arms and hands
                sb.Draw(Texture, new Rectangle(x - 13 * dir, y - 38, 7, 20), new Color(242, 207, 171));
                sb.Draw(Texture, new Rectangle(x + 9 * dir, y - 36, 7, 18), new Color(242, 207, 171));

                // Head + hair
                sb.Draw(Texture, new Rectangle(x - 9, y - 58, 18, 18), new Color(242, 207, 171));
                sb.Draw(Texture, new Rectangle(x - 10, y - 61, 20, 7), new Color(68, 44, 28));
                sb.Draw(Texture, new Rectangle(x + (FacingRight ? 1 : -1), y - 54, 4, 4), Color.Black);

                // Sword swing / held weapon
                if (IsSwinging)
                {
                    int swordX = FacingRight ? x + 14 : x - 36;
                    int hiltX = FacingRight ? x + 32 : x - 2;
                    sb.Draw(Texture, new Rectangle(swordX, y - 34, 22, 4), new Color(220, 220, 230));
                    sb.Draw(Texture, new Rectangle(hiltX, y - 37, 6, 10), new Color(255, 220, 100));
                }
                else
                {
                    int swordX = FacingRight ? x + 9 : x - 25;
                    int hiltX = FacingRight ? x + 20 : x - 5;
                    sb.Draw(Texture, new Rectangle(swordX, y - 34, 16, 3), new Color(220, 220, 230));
                    sb.Draw(Texture, new Rectangle(hiltX, y - 36, 4, 8), new Color(255, 220, 100));
                }
            }
        }
    }
}