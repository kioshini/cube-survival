using Project1.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Project1.Entities
{
    public sealed class Player
    {
        public TransformComponent Transform { get; }
        public MovementComponent Movement { get; }
        public SpriteComponent Sprite { get; }
        public HealthComponent Health { get; }
        public ColliderComponent Collider { get; }
        public ExperienceComponent Experience { get; }

        public Player(Vector2 position, Texture2D texture)
        {
            Transform = new TransformComponent(position, new Point(32, 32));
            Movement = new MovementComponent(speed: 200f);
            Sprite = new SpriteComponent(texture);
            Health = new HealthComponent(maxHp:100);
            Collider = new ColliderComponent(new Point(32, 32));
            Experience = new ExperienceComponent();
        }
    }

    public sealed class Enemy
    {
        public TransformComponent Transform { get; }
        public MovementComponent Movement { get; }
        public HealthComponent Health { get; }
        public ColliderComponent Collider { get; }
        public CombatStatsComponent CombatStats { get; }
        public SpriteComponent Sprite { get; }

        public Enemy(Vector2 position, Texture2D texture, int maxHP = 10, float speed = 80f)
        {
            Transform = new TransformComponent(position, new Point(24, 24));
            Movement = new MovementComponent(speed);
            Health = new HealthComponent(maxHP);
            Collider = new ColliderComponent(new Point(24, 24));
            CombatStats = new CombatStatsComponent(damage: 1);
            Sprite = new SpriteComponent(texture, Color.Red);
        }
    }

    public sealed class Projectile
    {
        public TransformComponent Transform { get; }
        public MovementComponent Movement { get; }
        public ColliderComponent Collider { get; }
        public int Damage { get; set; }
        public Color Color { get; set; }
        public bool IsActive { get; set; }
        public float Lifetime { get; set; }

        public Projectile()
        {
            Transform = new TransformComponent(Vector2.Zero, new Point(8, 8));
            Movement = new MovementComponent(400f);
            Collider = new ColliderComponent(new Point(8, 8));
            Damage = 1;
            Color = Color.Yellow;
            IsActive = false;
            Lifetime = 0f;
        }

        public void Initialize(Vector2 position, Vector2 direction, int damage, Color color,
            float speed, float lifetime = 10f)
        {
            Transform.Position = position;
            Movement.Speed = speed;
            Movement.Velocity = direction * speed;
            Damage = damage;
            Color = color;
            IsActive = true;
            Lifetime = lifetime;
        }

        public void Reset()
        {
            IsActive = false;
            Lifetime = 0f;
            Movement.Velocity = Vector2.Zero;
        }
    }

    public sealed class ExperienceDrop
    {
        public TransformComponent Transform { get; }
        public int Experience { get; }
        public bool IsActive { get; set; }
        public float LifeTime { get; set; }
        public const float MaxLifeTime = 30f;

        public ExperienceDrop(Vector2 position, int experience)
        {
            Transform = new TransformComponent(position, new Point(8, 8));
            Experience = experience;
            IsActive = true;
            LifeTime = MaxLifeTime;
        }

        public void Update(float dt)
        {
            LifeTime -= dt;
            if (LifeTime <= 0)
            {
                IsActive = false;
            }
        }
    }

    public sealed class Particle
    {
        public TransformComponent Transform { get; }
        public Vector2 Velocity { get; set; }
        public float Lifetime { get; set; }
        public float MaxLifetime { get; set; }
        public Color Color { get; set; }
        public bool IsActive { get; set; }

        public Particle()
        {
            Transform = new TransformComponent(Vector2.Zero, new Point(4, 4));
            Velocity = Vector2.Zero;
            Lifetime = 0f;
            MaxLifetime = 1f;
            Color = Color.White;
            IsActive = false;
        }

        public void Initialize(Vector2 position, Vector2 velocity, Color color, float lifetime)
        {
            Transform.Position = position;
            Velocity = velocity;
            Color = color;
            Lifetime = lifetime;
            MaxLifetime = lifetime;
            IsActive = true;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;

            Transform.Position += Velocity * dt;
            Lifetime -= dt;

            if (Lifetime <= 0)
                IsActive = false;
        }

        public float GetAlpha()
        {
            return Lifetime / MaxLifetime;
        }
    }

    public sealed class FloatingText
    {
        public string Text { get; }
        public Vector2 Position { get; private set; }
        public Color Color { get; }
        public bool IsActive => _lifetime > 0f;

        private readonly Vector2 _velocity;
        private readonly float _maxLifetime;
        private float _lifetime;

        public FloatingText(string text, Vector2 position, Color color, float lifetime = 0.75f)
        {
            Text = text;
            Position = position;
            Color = color;
            _velocity = new Vector2(0f, -45f);
            _maxLifetime = lifetime;
            _lifetime = lifetime;
        }

        public void Update(float dt)
        {
            if (!IsActive)
                return;

            Position += _velocity * dt;
            _lifetime -= dt;
        }

        public float GetAlpha()
        {
            return MathHelper.Clamp(_lifetime / _maxLifetime, 0f, 1f);
        }
    }
}
