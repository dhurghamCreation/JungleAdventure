using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using JungleAdventure.Source.Entities;

namespace JungleAdventure.Source.Systems
{
    public enum AIBehavior { Idle, Patrol, Chase, Attack, Flee, Dead }

    public class CombatManager
    {
        private List<DamageNumber> _damagePopups = new List<DamageNumber>();
        private Random _rng = new Random();

        public void Update(GameTime gameTime, Entity player, List<Entity> enemies)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive) continue;
                UpdateAIState(enemy, player, dt);
            }
            for (int i = _damagePopups.Count - 1; i >= 0; i--)
            {
                _damagePopups[i].Timer -= dt;
                _damagePopups[i].Position.Y -= 20f * dt;
                if (_damagePopups[i].Timer <= 0) _damagePopups.RemoveAt(i);
            }
        }

        private void UpdateAIState(Entity enemy, Entity player, float dt)
        {
            float distance = Vector2.Distance(enemy.Position, player.Position);
            switch (enemy.AIState)
            {
                case AIBehavior.Idle:
                    if (distance < 300f) enemy.AIState = AIBehavior.Chase;
                    break;
                case AIBehavior.Chase:
                    Vector2 dir = player.Position - enemy.Position;
                    dir.Normalize();
                    enemy.Velocity = dir * (enemy.Speed * 1.2f);
                    if (distance < 45f) enemy.AIState = AIBehavior.Attack;
                    if (distance > 500f) enemy.AIState = AIBehavior.Patrol;
                    break;
                case AIBehavior.Attack:
                    if (enemy.CanAttack) PerformAttack(enemy, player);
                    if (distance > 55f) enemy.AIState = AIBehavior.Chase;
                    break;
                case AIBehavior.Flee:
                    Vector2 fleeDir = enemy.Position - player.Position;
                    fleeDir.Normalize();
                    enemy.Velocity = fleeDir * (enemy.Speed * 1.5f);
                    break;
            }
        }

        public void PerformAttack(Entity attacker, Entity target)
        {
            float damage = attacker.BaseDamage;
            bool isCrit = _rng.NextDouble() < 0.15f;
            if (isCrit) damage *= 2.0f;
            target.TakeDamage(damage, attacker.Position);
            _damagePopups.Add(new DamageNumber(damage.ToString(), target.Position, isCrit ? Color.Yellow : Color.White));
            attacker.ResetAttackCooldown();
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (spriteBatch == null || font == null) return;
            foreach (var popup in _damagePopups)
                spriteBatch.DrawString(font, popup.Text, popup.Position, popup.Color, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        }
    }

    public class DamageNumber
    {
        public string Text;
        public Vector2 Position;
        public float Timer = 1.0f;
        public Color Color;

        public DamageNumber(string t, Vector2 p, Color c) { Text = t; Position = p; Color = c; }
    }
}