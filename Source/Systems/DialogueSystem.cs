using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using JungleAdventure.Source.Entities;
using JungleAdventure.Source.Entities.NPCs;
using System.Collections.Generic;

namespace JungleAdventure.Source.Systems
{
    public class DialogueSystem
    {
        public bool IsVisible { get; set; }
        private string _currentMessage;

        public void ShowMessage(string text)
        {
            _currentMessage = text;
            IsVisible = true;
        }

        public void Update(Player player, List<Entity> entities)
        {
            // Check for nearby NPCs to auto-trigger dialogue
            foreach (var entity in entities)
            {
                if (entity is Wizard wizard && wizard.IsActive)
                {
                    float dist = Vector2.Distance(player.Position, wizard.Position);
                    if (dist < 60f && !IsVisible)
                    {
                        ShowMessage("Welcome, adventurer! Explore the jungle and build your home.");
                    }
                    else if (dist >= 60f)
                    {
                        IsVisible = false;
                    }
                }
            }
        }

        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            if (!IsVisible || font == null) return;
            
            int screenW = 1280;
            int screenH = 720;
            
            // Semi-transparent box at the bottom
            Rectangle box = new Rectangle(100, screenH - 150, screenW - 200, 100);
            Texture2D pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { new Color(0, 0, 0, 200) });
            sb.Draw(pixel, box, Color.White * 0.8f);
            
            // Draw message text
            Vector2 textSize = font.MeasureString(_currentMessage);
            sb.DrawString(font, _currentMessage, new Vector2(
                (screenW - textSize.X) / 2f,
                screenH - 130
            ), Color.White);
        }
    }
}