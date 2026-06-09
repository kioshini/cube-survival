using Project1.Core;
using Project1.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Project1.Managers
{
    public sealed class GameStateManager
    {
        public GameState CurrentState { get; private set; }

        public GameStateManager()
        {
            CurrentState = GameState.Menu;
        }

        public void SetState(GameState state)
        {
            CurrentState = state;
        }

        public bool IsPlaying => CurrentState == GameState.Playing;
        public bool IsMenu => CurrentState == GameState.Menu;
        public bool IsStatistics => CurrentState == GameState.Statistics;
        public bool IsSkillTree => CurrentState == GameState.SkillTree;
        public bool IsPaused => CurrentState == GameState.Paused;
        public bool IsGameOver => CurrentState == GameState.GameOver;
    }

    public sealed class EntityManager
    {
        public Player Player { get; set; } = null!;
        public List<Enemy> Enemies { get; } = new();
        public List<ExperienceDrop> ExperienceDrops { get; } = new();
    }

    public sealed class ProjectilePool
    {
        private readonly List<Projectile> _projectiles;
        public IReadOnlyList<Projectile> Projectiles => _projectiles;

        public ProjectilePool(int capacity)
        {
            _projectiles = new List<Projectile>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                _projectiles.Add(new Projectile());
            }
        }

        public Projectile GetProjectile(Vector2 position, Vector2 direction, int damage, Color color,
            float speed, float lifetime = 10f)
        {
            foreach (var projectile in _projectiles)
            {
                if (!projectile.IsActive)
                {
                    projectile.Initialize(position, direction, damage, color, speed, lifetime);
                    return projectile;
                }
            }

            var newProjectile = new Projectile();
            newProjectile.Initialize(position, direction, damage, color, speed, lifetime);
            _projectiles.Add(newProjectile);
            return newProjectile;
        }
    }

    public sealed class EnemyFactory
    {
        private readonly Random _random;
        private readonly Texture2D _enemyTexture;

        public EnemyFactory(Random random, Texture2D enemyTexture)
        {
            _random = random;
            _enemyTexture = enemyTexture;
        }

        public Enemy CreateEnemy(Vector2 position, int difficultyLevel = 1)
        {
            var bonusHp = (difficultyLevel - 1) * 3;
            var bonusSpeed = System.Math.Min((difficultyLevel - 1) * 3f, 45f);

            return new Enemy(position, _enemyTexture, maxHP: 10 + bonusHp, speed: 80f + bonusSpeed);
        }

        public Vector2[] GetStandardSpawnPositions(Vector2 playerPosition, int viewportWidth,
            int viewportHeight, int margin = 120)
        {
            var positions = new Vector2[4];
            PopulateStandardSpawnPositions(playerPosition, viewportWidth, viewportHeight, positions, margin);
            return positions;
        }

        public void PopulateStandardSpawnPositions(Vector2 playerPosition, int viewportWidth,
            int viewportHeight, Vector2[] positions, int margin = 120)
        {
            if (positions == null || positions.Length < 4)
                throw new ArgumentException("Expected a buffer with at least four positions.", nameof(positions));

            var halfWidth = viewportWidth / 2f;
            var halfHeight = viewportHeight / 2f;

            positions[0] = new Vector2(playerPosition.X, playerPosition.Y - halfHeight - margin);
            positions[1] = new Vector2(playerPosition.X + halfWidth + margin, playerPosition.Y);
            positions[2] = new Vector2(playerPosition.X, playerPosition.Y + halfHeight + margin);
            positions[3] = new Vector2(playerPosition.X - halfWidth - margin, playerPosition.Y);
        }

        public Vector2 GetRandomSpawnPositionAround(Vector2 playerPosition, int viewportWidth,
            int viewportHeight, int margin = 120)
        {
            var halfWidth = viewportWidth / 2f;
            var halfHeight = viewportHeight / 2f;
            var side = _random.Next(4);

            return side switch
            {
                0 => new Vector2(
                    playerPosition.X + _random.Next((int)-halfWidth, (int)halfWidth),
                    playerPosition.Y - halfHeight - margin),
                1 => new Vector2(
                    playerPosition.X + halfWidth + margin,
                    playerPosition.Y + _random.Next((int)-halfHeight, (int)halfHeight)),
                2 => new Vector2(
                    playerPosition.X + _random.Next((int)-halfWidth, (int)halfWidth),
                    playerPosition.Y + halfHeight + margin),
                _ => new Vector2(
                    playerPosition.X - halfWidth - margin,
                    playerPosition.Y + _random.Next((int)-halfHeight, (int)halfHeight)),
            };
        }
    }
}
