using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Project1.Core.Components
{
    public sealed class TransformComponent
    {
        public Vector2 Position { get; set; }
        public Point Size { get; set; }
        public float Rotation { get; set; }

        public TransformComponent(Vector2 position, Point size)
        {
            Position = position;
            Size = size;
            Rotation = 0f;
        }

        public Rectangle GetBounds()
        {
            return BoundsHelper.CreateBounds(Position, Size);
        }
    }

    public sealed class SpriteComponent
    {
        public Texture2D Texture { get; set; }
        public Color Color { get; set; }

        public SpriteComponent(Texture2D texture, Color? color = null)
        {
            Texture = texture;
            Color = color ?? Color.White;
        }
    }

    public sealed class MovementComponent
    {
        public Vector2 Velocity { get; set; }
        public float Speed { get; set; }

        public MovementComponent(float speed)
        {
            Speed = speed;
            Velocity = Vector2.Zero;
        }
    }

    public sealed class HealthComponent
    {
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }

        public HealthComponent(int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void TakeDamage(int damage)
        {
            CurrentHp -= damage;
            if (CurrentHp < 0) { CurrentHp = 0; }
        }

        public void Heal(int amount)
        {
            CurrentHp += amount;
            if (CurrentHp > MaxHp) { CurrentHp = MaxHp; }
        }

        public void IncreaseMax(int amount)
        {
            MaxHp += amount;
            CurrentHp += amount;
        }

        public bool IsAlive => CurrentHp > 0;
    }

    public sealed class ExperienceComponent
    {
        public int Level { get; private set; }
        public int CurrentExperience { get; private set; }
        public int ExperienceToNextLevel { get; private set; }
        public int SkillPoints { get; private set; }

        public ExperienceComponent()
        {
            Level = 1;
            CurrentExperience = 0;
            ExperienceToNextLevel = GetExperienceRequiredForLevel(Level);
            SkillPoints = 0;
        }

        public bool AddExperience(int amount)
        {
            CurrentExperience += amount;
            var leveledUp = false;

            while (CurrentExperience >= ExperienceToNextLevel)
            {
                CurrentExperience -= ExperienceToNextLevel;
                LevelUp();
                leveledUp = true;
            }

            return leveledUp;
        }

        public void LevelUp()
        {
            Level++;
            SkillPoints++;
            ExperienceToNextLevel = GetExperienceRequiredForLevel(Level);
        }

        public bool SpendSkillPoint(int cost)
        {
            if (SkillPoints < cost)
                return false;

            SkillPoints -= cost;
            return true;
        }

        public float GetExperiencePercent()
        {
            return (float)CurrentExperience / ExperienceToNextLevel;
        }

        private int GetExperienceRequiredForLevel(int level)
        {
            return level switch
            {
                1 => 20,
                2 => 40,
                3 => 70,
                4 => 110,
                _ => 110 + (level - 4) * 55
            };
        }
    }

    public sealed class CombatStatsComponent
    {
        public int Damage { get; set; }

        public CombatStatsComponent(int damage = 1)
        {
            Damage = damage;
        }
    }

    public sealed class ColliderComponent
    {
        public Point Size { get; set; }

        public ColliderComponent(Point size)
        {
            Size = size;
        }

        public Rectangle GetBounds(Vector2 position)
        {
            return BoundsHelper.CreateBounds(position, Size);
        }
    }

    public static class BoundsHelper
    {
        public static Rectangle CreateBounds(Vector2 position, Point size)
        {
            return new Rectangle(
                (int)position.X - size.X / 2,
                (int)position.Y - size.Y / 2,
                size.X,
                size.Y);
        }
    }
}