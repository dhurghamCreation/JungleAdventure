using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JungleAdventure.Source.Entities;

namespace JungleAdventure.Source.Environment
{
    public class Tree : Entity
    {
        public int WoodYield = 15;
        public bool IsStump = false;
        private float _swayTimer;

        public Tree() : base()
        {
            Health = 50f;
            IsStatic = true;
        }

        public override void Update(GameTime gameTime, System.Collections.Generic.List<Entity> others)
        {
            // Trees sway in the wind logic
            _swayTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            Rotation = (float)System.Math.Sin(_swayTimer) * 0.05f;
            base.Update(gameTime, others);
        }

        public override void Die()
        {
            IsStump = true;
            IsCollidable = false;
            // Logic to drop wood items into the world
        }

        public override void Draw(SpriteBatch sb)
        {
            if (Texture != null)
                sb.Draw(Texture, Position, null, Color.White, Rotation, new Vector2(16, 16), 1.0f, SpriteEffects.None, 0f);
        }
    }
}