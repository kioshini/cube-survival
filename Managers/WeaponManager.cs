using Project1.Entities;
using Project1.Systems.Weapons;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Project1.Managers
{
    public sealed class WeaponManager
    {
        private readonly ProjectileWeapon _magicWeapon;
        private readonly ProjectileWeapon _gunWeapon;
        private readonly MeleeWeapon _meleeWeapon;

        public MeleeWeapon MeleeWeapon => _meleeWeapon;

        public WeaponManager(ProjectilePool projectilePool)
        {
            _meleeWeapon = new MeleeWeapon();
            _magicWeapon = new ProjectileWeapon(projectilePool, damage: 3, attackSpeed: 0.83f,
                range: 320f, projectileSpeed: 320f, projectileColor: Color.MediumPurple);
            _gunWeapon = new ProjectileWeapon(projectilePool, damage: 2, attackSpeed: 1.43f,
                range: 360f, projectileSpeed: 520f, projectileColor: Color.Yellow);
        }

        public void Update(GameTime gameTime, Vector2 playerPosition)
        {
            _meleeWeapon.Update(gameTime);
            _magicWeapon.Update(gameTime, playerPosition);
            _gunWeapon.Update(gameTime, playerPosition);
        }

        public void AutoAttack(Player player, List<Enemy> enemies, Vector2 facingDirection)
        {
            _meleeWeapon.AutoAttack(player, enemies, facingDirection);
            AutoAttackProjectileWeapon(_magicWeapon, player.Transform.Position, enemies);
            AutoAttackProjectileWeapon(_gunWeapon, player.Transform.Position, enemies);
        }

        public void ApplySwordPath() => _meleeWeapon.ApplySwordPath();
        public void ApplyBloodPath() => _meleeWeapon.ApplyBloodPath();
        public void UnlockMagicWeapon() => _magicWeapon.Unlock();
        public void UnlockGunWeapon() => _gunWeapon.Unlock();

        public void ImproveSwordReach()
        {
            _meleeWeapon.IncreaseRange(25f);
            _meleeWeapon.IncreaseMaxTargets(1);
        }

        public void ImproveSwordTempo()
        {
            _meleeWeapon.IncreaseDamage(2);
            _meleeWeapon.IncreaseAttackSpeed(0.25f);
        }

        public void ImproveBloodLeech()
        {
            _meleeWeapon.IncreaseHealOnKill(1);
            _meleeWeapon.IncreaseDamage(1);
        }

        public void ImproveBloodBody(Player player)
        {
            player.Health.IncreaseMax(20);
            _meleeWeapon.IncreaseHealOnKill(1);
        }

        public void ImproveMagicPower()
        {
            _magicWeapon.IncreaseDamage(2);
            _magicWeapon.IncreaseRange(40f);
        }

        public void ImproveMagicFlow()
        {
            _magicWeapon.IncreaseAttackSpeed(0.35f);
            _magicWeapon.IncreaseProjectileSpeed(80f);
        }

        public void ImproveGunReload()
        {
            _gunWeapon.IncreaseAttackSpeed(0.45f);
        }

        public void ImproveGunRounds()
        {
            _gunWeapon.IncreaseDamage(2);
            _gunWeapon.IncreaseProjectileSpeed(100f);
        }

        public void IncreaseProjectileDamage(int amount)
        {
            _magicWeapon.IncreaseDamage(amount);
            _gunWeapon.IncreaseDamage(amount);
        }

        public void IncreaseAttackSpeed(float amount)
        {
            _magicWeapon.IncreaseAttackSpeed(amount);
            _gunWeapon.IncreaseAttackSpeed(amount);
        }

        public void IncreaseProjectileSpeed(float amount)
        {
            _magicWeapon.IncreaseProjectileSpeed(amount);
            _gunWeapon.IncreaseProjectileSpeed(amount);
        }


        private void AutoAttackProjectileWeapon(ProjectileWeapon weapon, Vector2 playerPosition,
            List<Enemy> enemies)
        {
            if (!weapon.IsUnlocked || enemies.Count == 0)
                return;

            Enemy closestEnemy = null;
            var closestDistanceSquared = weapon.Range * weapon.Range;
            var closestDirection = Vector2.Zero;

            foreach (var enemy in enemies)
            {
                if (!enemy.Health.IsAlive)
                    continue;

                var direction = enemy.Transform.Position - playerPosition;
                var distanceSquared = direction.LengthSquared();
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestEnemy = enemy;
                    closestDirection = direction;
                }
            }

            if (closestEnemy == null)
                return;

            if (closestDirection.LengthSquared() > 0f)
                closestDirection.Normalize();

            weapon.Attack(playerPosition, closestDirection);
        }
    }
}
