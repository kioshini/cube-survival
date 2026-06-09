using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1.Entities;
using System.Collections.Generic;

namespace Project1.Managers
{
    public sealed class ChunkManager
    {
        public const int ChunkSize = 1024;
        public const int ActiveRadius = 1;

        private Point _centerChunk;
        private readonly List<Point> _activeChunks = new();
        private readonly Dictionary<Point, List<Enemy>> _enemiesByChunk = new();
        private readonly List<Point> _populatedEnemyChunks = new();

        public Point CenterChunk => _centerChunk;
        public IReadOnlyList<Point> ActiveChunks => _activeChunks;

        public ChunkManager()
        {
            Update(Vector2.Zero);
        }

        public void Update(Vector2 playerPosition)
        {
            var newCenter = GetChunkPosition(playerPosition);
            if (newCenter == _centerChunk && _activeChunks.Count > 0)
                return;

            _centerChunk = newCenter;
            RebuildActiveChunks();
        }

        public bool IsInsideActiveArea(Vector2 position)
        {
            var chunk = GetChunkPosition(position);
            return System.Math.Abs(chunk.X - _centerChunk.X) <= ActiveRadius &&
                   System.Math.Abs(chunk.Y - _centerChunk.Y) <= ActiveRadius;
        }

        public Rectangle GetChunkBounds(Point chunk)
        {
            return new Rectangle(chunk.X * ChunkSize, chunk.Y * ChunkSize, ChunkSize, ChunkSize);
        }

        public Point GetChunkPosition(Vector2 position)
        {
            return new Point(
                (int)System.Math.Floor(position.X / ChunkSize),
                (int)System.Math.Floor(position.Y / ChunkSize));
        }

        public void RebuildEnemyLookup(IReadOnlyList<Enemy> enemies)
        {
            for (var i = 0; i < _populatedEnemyChunks.Count; i++)
            {
                _enemiesByChunk[_populatedEnemyChunks[i]].Clear();
            }

            _populatedEnemyChunks.Clear();

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.Health.IsAlive)
                    continue;

                var chunk = GetChunkPosition(enemy.Transform.Position);
                if (!_enemiesByChunk.TryGetValue(chunk, out var chunkEnemies))
                {
                    chunkEnemies = new List<Enemy>();
                    _enemiesByChunk.Add(chunk, chunkEnemies);
                }

                if (chunkEnemies.Count == 0)
                    _populatedEnemyChunks.Add(chunk);

                chunkEnemies.Add(enemy);
            }
        }

        public bool TryGetEnemiesInChunk(Point chunk, out List<Enemy> enemies)
        {
            if (_enemiesByChunk.TryGetValue(chunk, out enemies) && enemies.Count > 0)
                return true;

            enemies = null;
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (var chunk in _activeChunks)
            {
                var rect = GetChunkBounds(chunk);
                var fillColor = chunk == _centerChunk ? Color.DarkSlateGray * 0.22f : Color.DarkSlateGray * 0.12f;
                spriteBatch.Draw(pixelTexture, rect, fillColor);
                DrawRectangleOutline(spriteBatch, pixelTexture, rect, Color.White * 0.16f);
            }
        }

        private void RebuildActiveChunks()
        {
            _activeChunks.Clear();

            for (var y = -ActiveRadius; y <= ActiveRadius; y++)
            {
                for (var x = -ActiveRadius; x <= ActiveRadius; x++)
                {
                    _activeChunks.Add(new Point(_centerChunk.X + x, _centerChunk.Y + y));
                }
            }
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), color);
        }
    }
}
