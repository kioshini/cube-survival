using Project1.Entities;
using Project1.Managers;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Project1.Systems.Weapons
{
    public sealed class MeleeWeapon
    {
        private float _attackCooldown;
        private float _attackSpeed;
        private int _damage;
        private float _range;
        private float _attackWidth;
        private int _maxTargets;
        private int _healOnKill;
        private float _slashTimer;
        private Enemy[] _targetBuffer = new Enemy[4];
        private float[] _targetDistanceBuffer = new float[4];

        public int Damage => _damage;
        public float Range => _range;
        public float AttackWidth => _attackWidth;
        public int MaxTargets => _maxTargets;
        public float SlashTimer => _slashTimer;
        public bool IsReady => _attackCooldown <= 0f;

        public MeleeWeapon()
        {
            _damage = 2;
            _attackSpeed = 1f;
            _range = 50f;
            _attackWidth = 54f;
            _maxTargets = 1;
            _healOnKill = 0;
            _attackCooldown = 0f;
        }

        public void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_attackCooldown > 0f)
                _attackCooldown -= dt;
            if (_slashTimer > 0f)
                _slashTimer -= dt;
        }

        public void AutoAttack(Player player, List<Enemy> enemies, Vector2 facingDirection)
        {
            if (_attackCooldown > 0f)
                return;

            var attackBounds = GetAttackBounds(player.Transform.Position, facingDirection);
            EnsureTargetBufferCapacity(_maxTargets);
            var targetCount = 0;

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.Health.IsAlive)
                    continue;

                if (!attackBounds.Intersects(enemy.Collider.GetBounds(enemy.Transform.Position)))
                    continue;

                var distanceSquared = Vector2.DistanceSquared(player.Transform.Position, enemy.Transform.Position);
                InsertTarget(enemy, distanceSquared, ref targetCount);
            }

            if (targetCount == 0)
                return;

            for (var i = 0; i < targetCount; i++)
            {
                var enemy = _targetBuffer[i];
                var wasAlive = enemy.Health.IsAlive;
                enemy.Health.TakeDamage(_damage);

                var knockbackDirection = GetEightWayDirection(facingDirection);
                enemy.Transform.Position += knockbackDirection * 12f;

                if (wasAlive && !enemy.Health.IsAlive && _healOnKill > 0)
                    player.Health.Heal(_healOnKill);
            }

            _slashTimer = 0.12f;
            _attackCooldown = 1f / _attackSpeed;
        }

        private void EnsureTargetBufferCapacity(int requiredCount)
        {
            if (_targetBuffer.Length >= requiredCount)
                return;

            var newSize = _targetBuffer.Length;
            while (newSize < requiredCount)
            {
                newSize *= 2;
            }

            System.Array.Resize(ref _targetBuffer, newSize);
            System.Array.Resize(ref _targetDistanceBuffer, newSize);
        }

        private void InsertTarget(Enemy enemy, float distanceSquared, ref int targetCount)
        {
            var insertIndex = targetCount;
            if (targetCount == _maxTargets && distanceSquared >= _targetDistanceBuffer[targetCount - 1])
                return;

            if (targetCount < _maxTargets)
                targetCount++;

            while (insertIndex > 0 && _targetDistanceBuffer[insertIndex - 1] > distanceSquared)
            {
                if (insertIndex < _maxTargets)
                {
                    _targetBuffer[insertIndex] = _targetBuffer[insertIndex - 1];
                    _targetDistanceBuffer[insertIndex] = _targetDistanceBuffer[insertIndex - 1];
                }

                insertIndex--;
            }

            if (insertIndex < _maxTargets)
            {
                _targetBuffer[insertIndex] = enemy;
                _targetDistanceBuffer[insertIndex] = distanceSquared;
            }
        }

        public Rectangle GetAttackBounds(Vector2 playerPosition, Vector2 facingDirection)
        {
            var direction = GetEightWayDirection(facingDirection);
            var width = (int)_attackWidth;
            var range = (int)_range;

            if (direction.X != 0f && direction.Y != 0f)
            {
                var x = direction.X > 0 ? (int)playerPosition.X : (int)playerPosition.X - range;
                var y1 = direction.Y > 0 ? (int)playerPosition.Y : (int)playerPosition.Y - range;
                return new Rectangle(x, y1, range, range);
            }

            if (System.Math.Abs(direction.X) > 0f)
            {
                var x = direction.X > 0 ? (int)playerPosition.X : (int)playerPosition.X - range;
                return new Rectangle(x, (int)playerPosition.Y - width / 2, range, width);
            }

            var y = direction.Y > 0 ? (int)playerPosition.Y : (int)playerPosition.Y - range;
            return new Rectangle((int)playerPosition.X - width / 2, y, width, range);
        }

        private Vector2 GetEightWayDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return new Vector2(0f, 1f);

            var normalized = direction;
            normalized.Normalize();

            var sector = (int)System.Math.Round(8d * System.Math.Atan2(normalized.Y, normalized.X) / (2d * System.Math.PI));
            sector = ((sector % 8) + 8) % 8;

            return sector switch
            {
                0 => new Vector2(1f, 0f),
                1 => new Vector2(1f, 1f),
                2 => new Vector2(0f, 1f),
                3 => new Vector2(-1f, 1f),
                4 => new Vector2(-1f, 0f),
                5 => new Vector2(-1f, -1f),
                6 => new Vector2(0f, -1f),
                7 => new Vector2(1f, -1f),
                _ => new Vector2(0f, 1f)
            };
        }

        public void ApplySwordPath()
        {
            _damage = System.Math.Max(_damage, 3);
            _range = System.Math.Max(_range, 75f);
            _attackWidth = System.Math.Max(_attackWidth, 70f);
            _maxTargets = System.Math.Max(_maxTargets, 2);
        }

        public void ApplyBloodPath()
        {
            _damage = System.Math.Max(_damage, 3);
            _healOnKill = System.Math.Max(_healOnKill, 1);
        }

        public void IncreaseDamage(int amount)
        {
            _damage += amount;
        }

        public void IncreaseRange(float amount)
        {
            _range += amount;
            _attackWidth += amount * 0.5f;
        }

        public void IncreaseMaxTargets(int amount)
        {
            _maxTargets += amount;
        }

        public void IncreaseAttackSpeed(float amount)
        {
            _attackSpeed += amount;
        }

        public void IncreaseHealOnKill(int amount)
        {
            _healOnKill += amount;
        }
    }

    public sealed class ProjectileWeapon
    {
        private readonly ProjectilePool _projectilePool;
        private float _attackCooldown;
        private float _attackSpeed;
        private int _damage;
        private float _projectileSpeed;
        private float _range;
        private Color _projectileColor;
        private bool _isUnlocked;

        public bool IsUnlocked => _isUnlocked;
        public float Range => _range;

        public ProjectileWeapon(ProjectilePool projectilePool, int damage, float attackSpeed,
            float range, float projectileSpeed, Color projectileColor, bool isUnlocked = false)
        {
            _projectilePool = projectilePool;
            _damage = damage;
            _attackSpeed = attackSpeed;
            _range = range;
            _projectileSpeed = projectileSpeed;
            _projectileColor = projectileColor;
            _isUnlocked = isUnlocked;
            _attackCooldown = 0f;
        }

        public void Update(GameTime gameTime, Vector2 playerPosition)
        {
            if (_attackCooldown > 0)
            {
                _attackCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        public void Attack(Vector2 playerPosition, Vector2 direction)
        {
            if (_isUnlocked && _attackCooldown <= 0 && direction != Vector2.Zero)
            {
                var normalizedDirection = direction;
                normalizedDirection.Normalize();

                _projectilePool.GetProjectile(playerPosition, normalizedDirection, _damage,
                    _projectileColor, _projectileSpeed);
                _attackCooldown = 1f / _attackSpeed;
            }
        }

        public void Unlock() => _isUnlocked = true;

        public void IncreaseAttackSpeed(float amount)
        {
            _attackSpeed += amount;
        }

        public void IncreaseDamage(int amount)
        {
            _damage += amount;
        }

        public void IncreaseProjectileSpeed(float amount)
        {
            _projectileSpeed += amount;
        }

        public void IncreaseRange(float amount)
        {
            _range += amount;
        }
    }
}
