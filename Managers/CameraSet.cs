using Microsoft.Xna.Framework;
using System;

namespace Project1.Managers
{
    public sealed class Camera2D
    {
        public Vector2 Position { get; set; }
        private int _screenWidth;
        private int _screenHeight;

        public Camera2D(int screenWidth, int screenHeight)
        {
            Position = Vector2.Zero;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
        }

        public void UpdateViewportSize(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
        }

        public void Follow(Vector2 targetPosition, float dt, float smoothSpeed = 5f)
        {
            var desiredPosition = targetPosition - new Vector2(_screenWidth / 2f, _screenHeight / 2f);
            Position = Vector2.Lerp(Position, desiredPosition, smoothSpeed * dt);
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0);
        }
    }

    public sealed class CameraShake
    {
        private Random _random;
        private float _shakeIntensity;
        private float _shakeDuration;
        private Vector2 _shakeOffset;

        public CameraShake(Random random)
        {
            _random = random;
            _shakeIntensity = 0f;
            _shakeDuration = 0f;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }

        public void Update(GameTime gameTime)
        {
            if (_shakeDuration <= 0)
            {
                _shakeOffset = Vector2.Zero;
                return;
            }

            _shakeDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            _shakeOffset = new Vector2(
                (float)(_random.NextDouble() - 0.5) * 2 * _shakeIntensity,
                (float)(_random.NextDouble() - 0.5) * 2 * _shakeIntensity);
        }

        public Vector2 GetOffset() => _shakeOffset;
    }
}