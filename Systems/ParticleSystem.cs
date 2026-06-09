using Microsoft.Xna.Framework;
using Project1.Entities;
using System;
using System.Collections.Generic;

namespace Project1.Systems
{
    public sealed class ParticleSystem
    {
        private readonly List<Particle> _particles;
        private readonly Random _random;
        private const int MaxParticles = 500;
        public IReadOnlyList<Particle> Particles => _particles;

        public ParticleSystem(Random random)
        {
            _random = random;
            _particles = new List<Particle>(MaxParticles);

            for (int i = 0; i < MaxParticles; i++)
            {
                _particles.Add(new Particle());
            }
        }

        public void Emit(Vector2 position, int count, Color color, float speed = 100f, float lifetime = 0.5f)
        {
            for (int i = 0; i < count; i++)
            {
                var particle = GetFreeParticle();
                if (particle == null) return;

                var angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
                var velocity = new Vector2(
                    (float)System.Math.Cos(angle) * speed,
                    (float)System.Math.Sin(angle) * speed);

                particle.Initialize(position, velocity, color, lifetime);
            }
        }

        private Particle GetFreeParticle()
        {
            foreach (var particle in _particles)
            {
                if (!particle.IsActive)
                    return particle;
            }
            return null;
        }

        public void Update(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (var i = 0; i < _particles.Count; i++)
            {
                var particle = _particles[i];
                if (particle.IsActive)
                    particle.Update(dt);
            }
        }
    }
}
