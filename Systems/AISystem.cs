using Project1.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Project1.Systems
{
    public sealed class AISystem
    {
        public void UpdateMovement(GameTime gameTime, Player player, List<Enemy> enemies)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var enemy in enemies)
            {
                if (!enemy.Health.IsAlive) { continue; }

                var direction = player.Transform.Position - enemy.Transform.Position;
                if (direction.LengthSquared() > 0) { direction.Normalize(); }

                enemy.Movement.Velocity = direction * enemy.Movement.Speed;
                enemy.Transform.Position += enemy.Movement.Velocity * dt;
            }
        }
    }
}
