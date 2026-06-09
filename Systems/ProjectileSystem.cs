using Project1.Entities;
using Project1.Managers;
using Project1.Systems.Weapons;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Project1.Systems
{
    public sealed class ProjectileSystem
    {
        private readonly ParticleSystem _particleSystem;

        public ProjectileSystem(ParticleSystem particleSystem)
        {
            _particleSystem = particleSystem;
        }

        public void Update(GameTime gameTime, ProjectilePool projectilePool, Vector2 playerPosition)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var projectiles = projectilePool.Projectiles;
            for (var i = 0; i < projectiles.Count; i++)
            {
                var projectile = projectiles[i];
                if (!projectile.IsActive) continue;

                projectile.Transform.Position += projectile.Movement.Velocity * dt;
                projectile.Lifetime -= dt;

                if (projectile.Lifetime <= 0 ||
                    Vector2.DistanceSquared(projectile.Transform.Position, playerPosition) > 1400f * 1400f)
                {
                    projectile.Reset();
                }
            }
        }

        public void CheckProjectileEnemyCollisions(ProjectilePool projectilePool, ChunkManager chunkManager,
            CameraShake cameraShake)
        {
            var projectiles = projectilePool.Projectiles;
            for (var i = 0; i < projectiles.Count; i++)
            {
                var projectile = projectiles[i];
                if (!projectile.IsActive)
                    continue;

                var projectileBounds = projectile.Collider.GetBounds(projectile.Transform.Position);
                var projectileChunk = chunkManager.GetChunkPosition(projectile.Transform.Position);
                var hitEnemy = false;

                for (var offsetY = -1; offsetY <= 1 && !hitEnemy; offsetY++)
                {
                    for (var offsetX = -1; offsetX <= 1 && !hitEnemy; offsetX++)
                    {
                        var chunk = new Point(projectileChunk.X + offsetX, projectileChunk.Y + offsetY);
                        if (!chunkManager.TryGetEnemiesInChunk(chunk, out var chunkEnemies))
                            continue;

                        for (var enemyIndex = 0; enemyIndex < chunkEnemies.Count; enemyIndex++)
                        {
                            var enemy = chunkEnemies[enemyIndex];
                            if (!enemy.Health.IsAlive)
                                continue;

                            var enemyBounds = enemy.Collider.GetBounds(enemy.Transform.Position);
                            if (!projectileBounds.Intersects(enemyBounds))
                                continue;

                            enemy.Health.TakeDamage(projectile.Damage);

                            _particleSystem.Emit(projectile.Transform.Position, 10, Color.Yellow,
                                speed: 100f, lifetime: 0.4f);

                            cameraShake.Shake(2f, 0.1f);

                            projectile.Reset();
                            hitEnemy = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
