using Project1.Entities;
using Microsoft.Xna.Framework;
using Project1.Managers;
using System.Collections.Generic;

namespace Project1.Systems
{
    public sealed class CombatSystem
    {
        private readonly Dictionary<Enemy, float> _enemyDamageCooldowns = new();
        private readonly List<PlayerDamageEvent> _playerDamageEvents = new();
        private readonly List<Enemy> _cooldownKeysBuffer = new();
        private readonly HashSet<Enemy> _activeEnemies = new();
        private const float DamageCooldownDuration = 0.5f;
        private readonly ParticleSystem _particleSystem;

        public IReadOnlyList<PlayerDamageEvent> PlayerDamageEvents => _playerDamageEvents;

        public CombatSystem(ParticleSystem particleSystem)
        {
            _particleSystem = particleSystem;
        }

        public void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            CollectCooldownKeys();
            for (var i = 0; i < _cooldownKeysBuffer.Count; i++)
            {
                var enemy = _cooldownKeysBuffer[i];
                var cooldown = _enemyDamageCooldowns[enemy] - dt;
                if (cooldown <= 0f)
                {
                    _enemyDamageCooldowns.Remove(enemy);
                    continue;
                }

                _enemyDamageCooldowns[enemy] = cooldown;
            }
        }

        public void CheckCollisions(Player player, IReadOnlyList<Enemy> enemies, ChunkManager chunkManager,
            GameTime gameTime)
        {
            var playerBounds = player.Transform.GetBounds();
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _playerDamageEvents.Clear();
            ClearUnusedCooldowns(enemies);

            var playerChunk = chunkManager.GetChunkPosition(player.Transform.Position);
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    var chunk = new Point(playerChunk.X + offsetX, playerChunk.Y + offsetY);
                    if (!chunkManager.TryGetEnemiesInChunk(chunk, out var chunkEnemies))
                        continue;

                    for (var i = 0; i < chunkEnemies.Count; i++)
                    {
                        var enemy = chunkEnemies[i];
                        if (!enemy.Health.IsAlive)
                            continue;

                        var enemyBounds = enemy.Collider.GetBounds(enemy.Transform.Position);
                        if (!playerBounds.Intersects(enemyBounds))
                            continue;

                        if (_enemyDamageCooldowns.TryGetValue(enemy, out var cooldown) && cooldown > 0f)
                            continue;

                        var damage = enemy.CombatStats.Damage;
                        player.Health.TakeDamage(damage);
                        _playerDamageEvents.Add(new PlayerDamageEvent(damage, player.Transform.Position));
                        _enemyDamageCooldowns[enemy] = DamageCooldownDuration;

                        var knockbackDirection = enemy.Transform.Position - player.Transform.Position;
                        if (knockbackDirection.LengthSquared() > 0f)
                            knockbackDirection.Normalize();
                        enemy.Transform.Position += knockbackDirection * 150f * dt;
                    }
                }
            }
        }

        public void CreateDeathParticles(Vector2 position)
        {
            _particleSystem.Emit(position, 20, Color.Red, speed: 150f, lifetime: 0.8f);
        }

        private void ClearUnusedCooldowns(IReadOnlyList<Enemy> enemies)
        {
            _activeEnemies.Clear();
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.Health.IsAlive)
                    _activeEnemies.Add(enemy);
            }

            CollectCooldownKeys();
            for (var i = 0; i < _cooldownKeysBuffer.Count; i++)
            {
                var enemy = _cooldownKeysBuffer[i];
                if (!enemy.Health.IsAlive || !_activeEnemies.Contains(enemy))
                    _enemyDamageCooldowns.Remove(enemy);
            }
        }

        private void CollectCooldownKeys()
        {
            _cooldownKeysBuffer.Clear();
            foreach (var enemy in _enemyDamageCooldowns.Keys)
            {
                _cooldownKeysBuffer.Add(enemy);
            }
        }

        public readonly struct PlayerDamageEvent
        {
            public int Damage { get; }
            public Vector2 Position { get; }

            public PlayerDamageEvent(int damage, Vector2 position)
            {
                Damage = damage;
                Position = position;
            }
        }
    }
}
