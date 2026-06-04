using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using JungleAdventure.Source.Systems;

namespace JungleAdventure.Source.Entities
{
    public abstract class Entity
    {
        // Identification
        public Guid ID = Guid.NewGuid();
        public string Name;
        public bool IsActive = true;

        // Transform & Physics
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float Scale = 1.0f;
        public Rectangle Hitbox;
        public bool IsCollidable = true;
        public bool IsStatic = false;

        // Combat Stats (FIXES COMBAT MANAGER ERRORS)
        public float Health = 100f;
        public float MaxHealth = 100f;
        public float BaseDamage = 10f;
        public float Speed = 100f;
        public bool CanAttack = true;
        public float AttackCooldown = 1.0f;
        private float _cooldownTimer = 0f;
        public AIBehavior AIState = AIBehavior.Idle; // Link to CombatManager enum

        // Visuals (FIXES "TEXTURE" ERRORS)
        public Texture2D Texture; 

        // Interaction
        public virtual void OnInteract(Player player) { }

        public abstract void Die();
        public abstract void Draw(SpriteBatch sb);

        public virtual void Update(GameTime gameTime, List<Entity> others)
        {
            if (!IsActive) return;

            // Handle Cooldowns
            if (_cooldownTimer > 0) _cooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            else CanAttack = true;
        }

        // Fixes the "No overload for TakeDamage takes 2 arguments" error
        public virtual void TakeDamage(float amount, Vector2 sourcePosition)
        {
            Health -= amount;
            // Add knockback logic here using sourcePosition
            if (Health <= 0) Die();
        }

        public void ResetAttackCooldown()
        {
            CanAttack = false;
            _cooldownTimer = AttackCooldown;
        }
    }
}