using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Project1.Entities;
using Project1.Managers;
using Project1.Systems;
using Project1.Core.UI;
using Project1.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Project1
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private EntityManager _entityManager;
        private Player _player;
        private Texture2D _pixelTexture;
        private EnemyFactory _enemyFactory;
        private AISystem _aiSystem;
        private CombatSystem _combatSystem;
        private ProjectileSystem _projectileSystem;
        private ParticleSystem _particleSystem;
        private WeaponManager _weaponManager;
        private ProjectilePool _projectilePool;
        private SkillTreeSystem _skillTreeSystem;
        private SkillTreeUI _skillTreeUI;
        private Camera2D _camera;
        private CameraShake _cameraShake;
        private ChunkManager _chunkManager;
        private GameStateManager _gameStateManager;
        private ProgressManager _progressManager;
        private Random _random;
        private KeyboardState _previousKeyboardState;
        private MouseState _previousMouseState;
        private Vector2 _facingDirection = new Vector2(0f, 1f);
        private float _spawnTimer;
        private float _survivalTime;
        private float _playerHitFlashTimer;
        private float _noDamageTimer;
        private int _vampireHealThisRun;
        private int _killCount;
        private List<FloatingText> _floatingTexts = new();
        private readonly Dictionary<Point, List<Enemy>> _crowdedEnemyGroups = new();
        private readonly List<Point> _activeCrowdingCells = new();
        private readonly Vector2[] _crowdingSpawnPositions = new Vector2[4];
        private readonly List<string> _wrappedStatisticsLines = new(4);
        private readonly StringBuilder _statisticsWrapLineBuilder = new();
        private readonly StringBuilder _statisticsWrapMeasureBuilder = new();
        private readonly string _skillDefinitionsPath = Path.Combine(AppContext.BaseDirectory, "Content", "skills.json");
        private SpriteFont _font;
        private const float BaseSpawnInterval = 1f;
        private const float MinSpawnInterval = 0.25f;
        private const int EnemyCrowdingThreshold = 10;
        private const float EnemyCrowdingCellSize = 64f;
        private const float EnemyScatterStep = 48f;
        private const float PlayerHitFlashDuration = 0.18f;
        private const float DespawnDistance = ChunkManager.ChunkSize * 1.8f;
        private const int BaseMaxEnemies = 20;
        private const int MenuPanelWidth = 520;
        private const int MenuPanelHeight = 340;
        private const int MenuButtonWidth = 260;
        private const int MenuButtonHeight = 44;
        private const int MenuButtonGap = 14;
        private const int WindowedBackBufferWidth = 1280;
        private const int WindowedBackBufferHeight = 720;
        private const float BaseUiWidth = 1280f;
        private const float BaseUiHeight = 720f;
        private bool _isHandlingResize;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            _graphics.HardwareModeSwitch = false;

            _graphics.PreferredBackBufferWidth = WindowedBackBufferWidth;
            _graphics.PreferredBackBufferHeight = WindowedBackBufferHeight;
        }

        protected override void Initialize()
        {
            Window.Title = "Cube survival";
            Window.ClientSizeChanged += OnClientSizeChanged;
            _random = new Random();
            _entityManager = new EntityManager();
            _aiSystem = new AISystem();
            _particleSystem = new ParticleSystem(_random);
            _combatSystem = new CombatSystem(_particleSystem);
            _projectileSystem = new ProjectileSystem(_particleSystem);
            _projectilePool = new ProjectilePool(capacity: 100);
            _weaponManager = new WeaponManager(_projectilePool);
            _camera = new Camera2D(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _cameraShake = new CameraShake(_random);
            _chunkManager = new ChunkManager();
            _gameStateManager = new GameStateManager();
            _progressManager = new ProgressManager();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });

            _player = new Player(
                new Vector2(GraphicsDevice.Viewport.Width / 2f,
                GraphicsDevice.Viewport.Height / 2f),
                _pixelTexture);

            _entityManager.Player = _player;
            _enemyFactory = new EnemyFactory(_random, _pixelTexture);

            try
            {
                _font = Content.Load<SpriteFont>("Arial");
            }
            catch
            {
                _font = null;
            }

            _gameStateManager.SetState(GameState.Menu);
            _previousKeyboardState = Keyboard.GetState();
            _previousMouseState = Mouse.GetState();
            UpdateViewportDependentState();
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            var uiScale = GetUiScale();

            if (IsKeyPressed(keyboardState, Keys.F11))
            {
                ToggleFullscreenWindowed();
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsPlaying &&
                (IsKeyPressed(keyboardState, Keys.Escape) || IsKeyPressed(keyboardState, Keys.P)))
            {
                if (_skillTreeUI.IsActive)
                    _skillTreeUI.Hide();

                _gameStateManager.SetState(GameState.Paused);
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsMenu)
            {
                UpdateMainMenu(keyboardState, mouseState, uiScale);
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsStatistics)
            {
                UpdateStatisticsMenu(keyboardState, mouseState, uiScale);
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsPaused)
            {
                UpdatePauseMenu(keyboardState, mouseState, uiScale);
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsGameOver)
            {
                if (IsKeyPressed(keyboardState, Keys.R))
                    StartNewRun();
                if (IsKeyPressed(keyboardState, Keys.Escape))
                    _gameStateManager.SetState(GameState.Menu);

                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (_gameStateManager.IsSkillTree)
            {
                if (IsKeyPressed(keyboardState, Keys.Escape) || IsKeyPressed(keyboardState, Keys.P))
                {
                    CloseSkillTreeAndPause();
                    _previousKeyboardState = keyboardState;
                    _previousMouseState = mouseState;
                    base.Update(gameTime);
                    return;
                }

                if (IsKeyPressed(keyboardState, Keys.Tab))
                {
                    CloseSkillTreeAndResume();
                    _previousKeyboardState = keyboardState;
                    _previousMouseState = mouseState;
                    base.Update(gameTime);
                    return;
                }

                _skillTreeUI.Update(keyboardState, _skillTreeSystem, _player, _weaponManager);
                UpdateBranchAchievements();
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (IsKeyPressed(keyboardState, Keys.Tab))
            {
                OpenSkillTree();
                _previousKeyboardState = keyboardState;
                _previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _survivalTime += dt;
            if (_playerHitFlashTimer > 0f)
                _playerHitFlashTimer -= dt;
            UpdateFloatingTexts(dt);

            var direction = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.W))
                direction.Y -= 1;
            if (keyboardState.IsKeyDown(Keys.S))
                direction.Y += 1;
            if (keyboardState.IsKeyDown(Keys.A))
                direction.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D))
                direction.X += 1;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _facingDirection = direction;
            }

            _player.Movement.Velocity = direction * _player.Movement.Speed;
            _player.Transform.Position += _player.Movement.Velocity * dt;
            _chunkManager.Update(_player.Transform.Position);

            _spawnTimer += dt;
            var difficultyLevel = GetDifficultyLevel();
            if (_spawnTimer >= GetCurrentSpawnInterval() &&
                _entityManager.Enemies.Count < GetCurrentMaxEnemies())
            {
                var spawnPosition = _enemyFactory.GetRandomSpawnPositionAround(
                    _player.Transform.Position,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
                _entityManager.Enemies.Add(_enemyFactory.CreateEnemy(spawnPosition, difficultyLevel));
                _spawnTimer = 0f;
            }

            _weaponManager.Update(gameTime, _player.Transform.Position);

            var hpBeforeAttack = _player.Health.CurrentHp;
            _weaponManager.AutoAttack(_player, _entityManager.Enemies, _facingDirection);
            var vampireHeal = _player.Health.CurrentHp - hpBeforeAttack;
            if (vampireHeal > 0)
            {
                _vampireHealThisRun += vampireHeal;
                if (_vampireHealThisRun >= 50)
                    _progressManager.UnlockAchievement("vampire_heal_50");
            }

            _projectileSystem.Update(gameTime, _projectilePool, _player.Transform.Position);
            _particleSystem.Update(gameTime);
            _aiSystem.UpdateMovement(gameTime, _player, _entityManager.Enemies);
            RedistributeOvercrowdedEnemies();
            _combatSystem.Update(gameTime);
            _chunkManager.RebuildEnemyLookup(_entityManager.Enemies);
            _combatSystem.CheckCollisions(_player, _entityManager.Enemies, _chunkManager, gameTime);
            var tookDamage = HandlePlayerDamageFeedback();
            if (tookDamage)
            {
                _noDamageTimer = 0f;
            }
            else
            {
                _noDamageTimer += dt;
                if (_noDamageTimer >= 300f)
                    _progressManager.UnlockAchievement("no_damage_5min");
            }

            _projectileSystem.CheckProjectileEnemyCollisions(_projectilePool, _chunkManager, _cameraShake);
            _cameraShake.Update(gameTime);
            _camera.Follow(_player.Transform.Position, dt);
            RemoveObjectsOutsideActiveArea();

            for (int i = _entityManager.Enemies.Count - 1; i >= 0; i--)
            {
                if (!_entityManager.Enemies[i].Health.IsAlive)
                {
                    var dropPosition = _entityManager.Enemies[i].Transform.Position;
                    _entityManager.ExperienceDrops.Add(new ExperienceDrop(dropPosition, 10));
                    _combatSystem.CreateDeathParticles(dropPosition);
                    _entityManager.Enemies.RemoveAt(i);
                    _killCount++;
                    _progressManager.RecordLifetimeKill();
                }
            }

            for (int i = _entityManager.ExperienceDrops.Count - 1; i >= 0; --i)
            {
                _entityManager.ExperienceDrops[i].Update(dt);
                if (!_entityManager.ExperienceDrops[i].IsActive)
                {
                    _entityManager.ExperienceDrops.RemoveAt(i);
                    continue;
                }

                var dropBounds = new Rectangle(
                    (int)_entityManager.ExperienceDrops[i].Transform.Position.X - 4,
                    (int)_entityManager.ExperienceDrops[i].Transform.Position.Y - 4, 8, 8);
                var playerBounds = _player.Transform.GetBounds();

                if (dropBounds.Intersects(playerBounds))
                {
                    _player.Experience.AddExperience(_entityManager.ExperienceDrops[i].Experience);
                    _entityManager.ExperienceDrops.RemoveAt(i);

                    if (_player.Experience.Level >= 15)
                        _progressManager.UnlockAchievement("level_15_run");
                }
            }

            UpdateRunMilestoneAchievements();

            if (!_player.Health.IsAlive)
            {
                RecordRunProgress();
                _gameStateManager.SetState(GameState.GameOver);
            }

            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            var uiScale = GetUiScale();

            if (_gameStateManager.IsMenu)
            {
                _spriteBatch.Begin();
                DrawMainMenu(uiScale);
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            if (_gameStateManager.IsStatistics)
            {
                _spriteBatch.Begin();
                DrawStatisticsMenu(uiScale);
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            var cameraMatrix = _camera.GetViewMatrix();
            var shakeOffset = _cameraShake.GetOffset();

            _spriteBatch.Begin(transformMatrix: cameraMatrix * Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y, 0));

            _chunkManager.Draw(_spriteBatch, _pixelTexture);

            var playerBounds = _player.Transform.GetBounds();
            var playerColor = _playerHitFlashTimer > 0f
                ? Color.Lerp(_player.Sprite.Color, Color.Red, _playerHitFlashTimer / PlayerHitFlashDuration)
                : _player.Sprite.Color;

            _spriteBatch.Draw(
                _player.Sprite.Texture,
                playerBounds,
                null,
                playerColor,
                _player.Transform.Rotation,
                Vector2.Zero,
                SpriteEffects.None,
                0f);

            DrawHealthBar(_spriteBatch, _player.Transform.Position,
                _player.Health.CurrentHp, _player.Health.MaxHp, uiScale, 25f);


            var projectiles = _projectilePool.Projectiles;
            for (var i = 0; i < projectiles.Count; i++)
            {
                var projectile = projectiles[i];
                if (!projectile.IsActive)
                    continue;

                var projectileBounds = projectile.Collider.GetBounds(projectile.Transform.Position);
                _spriteBatch.Draw(_pixelTexture, projectileBounds, null, projectile.Color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }

            DrawMeleeAttackArea();

            if (_weaponManager.MeleeWeapon.SlashTimer > 0f)
            {
                var slashBounds = _weaponManager.MeleeWeapon.GetAttackBounds(
                    _player.Transform.Position, _facingDirection);
                _spriteBatch.Draw(_pixelTexture, slashBounds, Color.White * 0.22f);
                DrawRectangleOutline(_spriteBatch, slashBounds, Color.White * 0.55f);
            }

         

            foreach (var drop in _entityManager.ExperienceDrops)
            {
                var dropBounds = new Rectangle(
                    (int)drop.Transform.Position.X - 4,
                    (int)drop.Transform.Position.Y - 4, 8, 8);
                _spriteBatch.Draw(_pixelTexture, dropBounds, Color.Cyan);
            }

            foreach (var enemy in _entityManager.Enemies)
            {
                var enemyBounds = enemy.Transform.GetBounds();
                _spriteBatch.Draw(
                    enemy.Sprite.Texture,
                    enemyBounds,
                    null,
                    enemy.Sprite.Color,
                    enemy.Transform.Rotation,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f);

                DrawHealthBar(_spriteBatch, enemy.Transform.Position,
                    enemy.Health.CurrentHp, enemy.Health.MaxHp, uiScale, 20f);
            }

            var particles = _particleSystem.Particles;
            for (var i = 0; i < particles.Count; i++)
            {
                var particle = particles[i];
                if (!particle.IsActive)
                    continue;

                var particleBounds = new Rectangle(
                    (int)particle.Transform.Position.X - 2,
                    (int)particle.Transform.Position.Y - 2, 4, 4);
                var alpha = particle.GetAlpha();
                _spriteBatch.Draw(_pixelTexture, particleBounds, particle.Color * alpha);
            }

            if (_font != null)
            {
                foreach (var floatingText in _floatingTexts)
                {
                    _spriteBatch.DrawString(_font, floatingText.Text, floatingText.Position,
                        floatingText.Color * floatingText.GetAlpha(), 0f, Vector2.Zero,
                        Vector2.One * uiScale, SpriteEffects.None, 0f);
                }
            }

            _spriteBatch.End();

            _spriteBatch.Begin();

            DrawScreenBars(uiScale);

            if (_font != null)
            {
                _spriteBatch.DrawString(_font, $"Уровень: {_player.Experience.Level}",
                    ScaleVector(new Vector2(10, 64), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, $"Враги: {_entityManager.Enemies.Count}",
                    ScaleVector(new Vector2(10, 94), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, $"Убийства: {_killCount}",
                    ScaleVector(new Vector2(10, 124), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, $"Время: {FormatTime(_survivalTime)}",
                    ScaleVector(new Vector2(560, 10), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font, $"Очки навыков: {_player.Experience.SkillPoints}",
                    ScaleVector(new Vector2(10, 184), uiScale),
                    _player.Experience.SkillPoints > 0 ? Color.Gold : Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);

                if (_survivalTime < 5f && !_gameStateManager.IsGameOver)
                {
                    _spriteBatch.DrawString(_font, "WASD - Движение   Tab - Дерево навыков   P - Пауза",
                        ScaleVector(new Vector2(520, 650), uiScale), Color.White * (1f - _survivalTime / 5f),
                        0f, Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                }

                if (_player.Experience.SkillPoints > 0 && !_gameStateManager.IsGameOver)
                {
                    _spriteBatch.DrawString(_font, "Tab - Дерево навыков",
                        ScaleVector(new Vector2(560, 44), uiScale), Color.Gold, 0f,
                        Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                }

                if (_gameStateManager.IsPaused)
                {
                    DrawPauseMenu(uiScale);
                }

                if (_gameStateManager.IsGameOver)
                {
                    DrawGameOverPanel(uiScale);
                }
            }

            _skillTreeUI.Draw(_spriteBatch, _font, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, uiScale, _skillTreeSystem, _player);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void StartNewRun()
        {
            _entityManager = new EntityManager();
            _particleSystem = new ParticleSystem(_random);
            _combatSystem = new CombatSystem(_particleSystem);
            _projectileSystem = new ProjectileSystem(_particleSystem);
            _projectilePool = new ProjectilePool(capacity: 100);
            _weaponManager = new WeaponManager(_projectilePool);
            _skillTreeSystem = CreateSkillTreeSystem();
            _skillTreeUI = new SkillTreeUI(_pixelTexture);
            _cameraShake = new CameraShake(_random);
            _chunkManager = new ChunkManager();

            _player = new Player(Vector2.Zero, _pixelTexture);
            _entityManager.Player = _player;
            _camera.Position = Vector2.Zero;
            _chunkManager.Update(_player.Transform.Position);

            _spawnTimer = 0f;
            _survivalTime = 0f;
            _playerHitFlashTimer = 0f;
            _noDamageTimer = 0f;
            _vampireHealThisRun = 0;
            _killCount = 0;
            _facingDirection = new Vector2(0f, 1f);
            _floatingTexts.Clear();
            ClearCrowdedEnemyGroups();
            _previousKeyboardState = Keyboard.GetState();
            _previousMouseState = Mouse.GetState();

            _gameStateManager.SetState(GameState.Playing);
        }

        private void RecordRunProgress()
        {
            _progressManager.RecordRunSummary(_killCount, _survivalTime, _player.Experience.Level);
        }

        private void OpenSkillTree()
        {
            _skillTreeUI.Show();
            _gameStateManager.SetState(GameState.SkillTree);
        }

        private void CloseSkillTreeAndResume()
        {
            _skillTreeUI.Hide();
            _gameStateManager.SetState(GameState.Playing);
        }

        private void CloseSkillTreeAndPause()
        {
            _skillTreeUI.Hide();
            _gameStateManager.SetState(GameState.Paused);
        }

        private void UpdateMainMenu(KeyboardState keyboardState, MouseState mouseState, float uiScale)
        {
            var playButton = GetCenteredMenuButtonRect(0, uiScale);
            var statisticsButton = GetCenteredMenuButtonRect(1, uiScale);
            var exitButton = GetCenteredMenuButtonRect(2, uiScale);

            if (IsMenuActivatePressed(keyboardState, mouseState, playButton))
            {
                StartNewRun();
                return;
            }

            if (IsMenuActivatePressed(keyboardState, mouseState, statisticsButton))
            {
                _gameStateManager.SetState(GameState.Statistics);
                return;
            }

            if (IsMouseClicked(mouseState) && exitButton.Contains(mouseState.Position))
            {
                Exit();
                return;
            }

            if (IsKeyPressed(keyboardState, Keys.Escape))
                Exit();
        }

        private void UpdatePauseMenu(KeyboardState keyboardState, MouseState mouseState, float uiScale)
        {
            var continueButton = GetCenteredMenuButtonRect(0, uiScale);
            var restartButton = GetCenteredMenuButtonRect(1, uiScale);
            var menuButton = GetCenteredMenuButtonRect(2, uiScale);

            if (IsKeyPressed(keyboardState, Keys.Escape) ||
                IsKeyPressed(keyboardState, Keys.P) ||
                IsMenuActivatePressed(keyboardState, mouseState, continueButton))
            {
                _gameStateManager.SetState(GameState.Playing);
                return;
            }

            if (IsMenuActivatePressed(keyboardState, mouseState, restartButton))
            {
                StartNewRun();
                return;
            }

            if (IsMenuActivatePressed(keyboardState, mouseState, menuButton))
            {
                _gameStateManager.SetState(GameState.Menu);
            }
        }

        private void UpdateStatisticsMenu(KeyboardState keyboardState, MouseState mouseState, float uiScale)
        {
            var backButton = GetStatisticsBackButtonRect(uiScale);

            if (IsMenuActivatePressed(keyboardState, mouseState, backButton) || IsKeyPressed(keyboardState, Keys.Escape))
                _gameStateManager.SetState(GameState.Menu);
        }

        private bool IsMenuActivatePressed(KeyboardState keyboardState, MouseState mouseState, Rectangle buttonRect)
        {
            return IsKeyPressed(keyboardState, Keys.Enter) ||
                   IsKeyPressed(keyboardState, Keys.Space) ||
                   IsMouseClicked(mouseState) && buttonRect.Contains(mouseState.Position);
        }

        private bool IsMouseClicked(MouseState mouseState)
        {
            return mouseState.LeftButton == ButtonState.Pressed &&
                   _previousMouseState.LeftButton == ButtonState.Released;
        }

        private Rectangle GetCenteredMenuButtonRect(int index, float uiScale)
        {
            var panelRect = GetMenuPanelRect(uiScale);
            var buttonWidth = ScaleLength(MenuButtonWidth, uiScale);
            var buttonHeight = ScaleLength(MenuButtonHeight, uiScale);
            var buttonX = panelRect.X + (panelRect.Width - buttonWidth) / 2;
            var buttonY = panelRect.Y + ScaleLength(110 + index * (MenuButtonHeight + MenuButtonGap), uiScale);

            return new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);
        }

        private Rectangle GetMenuPanelRect(float uiScale)
        {
            var panelWidth = ScaleLength(MenuPanelWidth, uiScale);
            var panelHeight = ScaleLength(MenuPanelHeight, uiScale);
            return new Rectangle((GraphicsDevice.Viewport.Width - panelWidth) / 2,
                (GraphicsDevice.Viewport.Height - panelHeight) / 2, panelWidth, panelHeight);
        }

        private Rectangle GetStatisticsBackButtonRect(float uiScale)
        {
            var panelWidth = ScaleLength(1120, uiScale);
            var panelHeight = ScaleLength(620, uiScale);
            var panelRect = new Rectangle((GraphicsDevice.Viewport.Width - panelWidth) / 2,
                (GraphicsDevice.Viewport.Height - panelHeight) / 2, panelWidth, panelHeight);
            var buttonWidth = ScaleLength(220, uiScale);
            var buttonHeight = ScaleLength(44, uiScale);

            return new Rectangle(panelRect.Right - buttonWidth - ScaleLength(28, uiScale),
                panelRect.Bottom - buttonHeight - ScaleLength(24, uiScale), buttonWidth, buttonHeight);
        }

        private void DrawMainMenu(float uiScale)
        {
            DrawMenuOverlay("Cube survival", "Арена выживания", uiScale,
                "Играть", "Статистика", "Выход");
        }

        private void DrawPauseMenu(float uiScale)
        {
            DrawMenuOverlay("ПАУЗА", "Esc/P или клик по кнопке Продолжить   F11 - Полный экран", uiScale,
                "Продолжить", "Заново", "Главное меню");
        }

        private void DrawMenuOverlay(string title, string subtitle, float uiScale, params string[] buttonLabels)
        {
            var screenWidth = GraphicsDevice.Viewport.Width;
            var screenHeight = GraphicsDevice.Viewport.Height;
            var panelRect = GetMenuPanelRect(uiScale);

            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.72f);
            _spriteBatch.Draw(_pixelTexture, panelRect, Color.DarkSlateGray);
            DrawRectangleOutline(_spriteBatch, panelRect, Color.White, ScaleLength(2, uiScale));

            if (_font == null)
                return;

            var titleSize = _font.MeasureString(title) * uiScale;
            var titlePosition = new Vector2(panelRect.Center.X - titleSize.X / 2f, panelRect.Y + ScaleLength(24, uiScale));
            _spriteBatch.DrawString(_font, title, titlePosition, Color.White, 0f, Vector2.Zero,
                Vector2.One * uiScale, SpriteEffects.None, 0f);

            var subtitleSize = _font.MeasureString(subtitle) * uiScale;
            var subtitlePosition = new Vector2(panelRect.Center.X - subtitleSize.X / 2f,
                panelRect.Y + ScaleLength(64, uiScale));
            _spriteBatch.DrawString(_font, subtitle, subtitlePosition, Color.LightGray, 0f, Vector2.Zero,
                Vector2.One * uiScale, SpriteEffects.None, 0f);

            for (var i = 0; i < buttonLabels.Length; i++)
            {
                var buttonRect = GetCenteredMenuButtonRect(i, uiScale);
                DrawMenuButton(buttonRect, buttonLabels[i], IsMenuActivateHovered(buttonRect), uiScale);
            }
        }

        private void DrawStatisticsMenu(float uiScale)
        {
            var screenWidth = GraphicsDevice.Viewport.Width;
            var screenHeight = GraphicsDevice.Viewport.Height;
            var panelWidth = ScaleLength(1120, uiScale);
            var panelHeight = ScaleLength(620, uiScale);
            var panelRect = new Rectangle((screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2, panelWidth, panelHeight);

            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.72f);
            _spriteBatch.Draw(_pixelTexture, panelRect, Color.DarkSlateGray);
            DrawRectangleOutline(_spriteBatch, panelRect, Color.White, ScaleLength(2, uiScale));

            if (_font == null)
                return;

            var titlePosition = new Vector2(panelRect.X + ScaleLength(24, uiScale), panelRect.Y + ScaleLength(18, uiScale));
            _spriteBatch.DrawString(_font, "СТАТИСТИКА", titlePosition, Color.White, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);

            var subtitlePosition = new Vector2(panelRect.X + ScaleLength(24, uiScale), panelRect.Y + ScaleLength(54, uiScale));
            _spriteBatch.DrawString(_font, "Esc или Назад - в меню", subtitlePosition, Color.LightGray, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);

            var progress = _progressManager.Progress;
            var leftX = panelRect.X + ScaleLength(32, uiScale) + 40;
            var rightX = panelRect.X + ScaleLength(560, uiScale);

            var statisticsStartY = panelRect.Y + ScaleLength(150, uiScale);
            var achievementsStartY = panelRect.Y + ScaleLength(120, uiScale);
            var lineGap = ScaleLength(30, uiScale);
            var achievementColumnWidth = ScaleLength(260, uiScale);
            var achievementColumnGap = ScaleLength(18, uiScale);
            var achievementRowHeight = ScaleLength(70, uiScale);
            var achievementRowsPerColumn = 6;
            var achievementTextScale = uiScale * 0.82f;

            DrawStatisticsLine($"Забегов сыграно: {progress.RunsPlayed}", leftX, statisticsStartY, uiScale);
            DrawStatisticsLine($"Всего убийств: {progress.TotalKills}", leftX, statisticsStartY + lineGap, uiScale);
            DrawStatisticsLine($"Лучший забег по убийствам: {progress.BestRunKills}", leftX, statisticsStartY + lineGap * 2, uiScale);
            DrawStatisticsLine($"Лучшее выживание: {_progressManager.FormatSurvivalTime(progress.BestSurvivalSeconds)}", leftX, statisticsStartY + lineGap * 3, uiScale);
            DrawStatisticsLine($"Макс. уровень: {progress.HighestLevelReached}", leftX, statisticsStartY + lineGap * 4, uiScale);

            _spriteBatch.DrawString(_font,"ДОСТИЖЕНИЯ",new Vector2(rightX, statisticsStartY-ScaleLength(50,uiScale)),Color.White,0f,Vector2.Zero,Vector2.One * uiScale,SpriteEffects.None,0f);

            var achievements = _progressManager.GetAchievementViews();
            for (var i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];
                var columnIndex = i / achievementRowsPerColumn;
                var rowIndex = i % achievementRowsPerColumn;
                var x = rightX + columnIndex * (achievementColumnWidth + achievementColumnGap);
                var y = achievementsStartY + rowIndex * achievementRowHeight;
                var statePosition = new Vector2(x, y);
                var achievementTitlePosition = new Vector2(x, y + ScaleLength(18, uiScale));
                var descriptionPosition = new Vector2(x, y + ScaleLength(42, uiScale));
                var nameColor = achievement.IsUnlocked ? Color.LimeGreen : Color.Gray;
                var descriptionColor = achievement.IsUnlocked ? Color.White : Color.LightGray;
                var stateText = achievement.IsUnlocked ? "Открыто" : "Закрыто";
                var stateColor = achievement.IsUnlocked ? Color.Gold : Color.IndianRed;

                _spriteBatch.DrawString(_font, achievement.Name, achievementTitlePosition, nameColor, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                DrawWrappedStatisticsString(achievement.Description, descriptionPosition, descriptionColor,
                    achievementColumnWidth - ScaleLength(14, uiScale), 2, achievementTextScale);
                _spriteBatch.DrawString(_font, stateText, statePosition, stateColor, 0f,
                    Vector2.Zero, Vector2.One * achievementTextScale, SpriteEffects.None, 0f);
            }

            DrawMenuButton(GetStatisticsBackButtonRect(uiScale), "Назад",
                IsMenuActivateHovered(GetStatisticsBackButtonRect(uiScale)), uiScale);
        }

        private void DrawStatisticsLine(string text, int x, int y, float uiScale)
        {
            _spriteBatch.DrawString(_font, text, new Vector2(x, y), Color.White, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
        }

        private void DrawWrappedStatisticsString(string text, Vector2 position, Color color, float maxWidth,
            int maxLines, float textScale)
        {
            WrapText(_font, text, maxWidth, maxLines, textScale);
            for (var i = 0; i < _wrappedStatisticsLines.Count; i++)
            {
                _spriteBatch.DrawString(_font, _wrappedStatisticsLines[i],
                    position + new Vector2(0f, _font.LineSpacing * textScale * i),
                    color, 0f, Vector2.Zero, Vector2.One * textScale, SpriteEffects.None, 0f);
            }
        }

        private void WrapText(SpriteFont font, string text, float maxWidth, int maxLines, float textScale)
        {
            _wrappedStatisticsLines.Clear();
            _statisticsWrapLineBuilder.Clear();
            _statisticsWrapMeasureBuilder.Clear();

            for (var index = 0; index < text.Length;)
            {
                while (index < text.Length && text[index] == ' ')
                {
                    index++;
                }

                if (index >= text.Length)
                    break;

                var wordStart = index;
                while (index < text.Length && text[index] != ' ')
                {
                    index++;
                }

                var wordLength = index - wordStart;
                _statisticsWrapMeasureBuilder.Clear();
                _statisticsWrapMeasureBuilder.Append(_statisticsWrapLineBuilder);
                if (_statisticsWrapMeasureBuilder.Length > 0)
                    _statisticsWrapMeasureBuilder.Append(' ');
                _statisticsWrapMeasureBuilder.Append(text, wordStart, wordLength);

                if (font.MeasureString(_statisticsWrapMeasureBuilder).X * textScale <= maxWidth)
                {
                    if (_statisticsWrapLineBuilder.Length > 0)
                        _statisticsWrapLineBuilder.Append(' ');
                    _statisticsWrapLineBuilder.Append(text, wordStart, wordLength);
                    continue;
                }

                if (_statisticsWrapLineBuilder.Length > 0)
                    _wrappedStatisticsLines.Add(_statisticsWrapLineBuilder.ToString());

                if (_wrappedStatisticsLines.Count >= maxLines)
                    return;

                _statisticsWrapLineBuilder.Clear();
                _statisticsWrapLineBuilder.Append(text, wordStart, wordLength);
            }

            if (_statisticsWrapLineBuilder.Length > 0 && _wrappedStatisticsLines.Count < maxLines)
                _wrappedStatisticsLines.Add(_statisticsWrapLineBuilder.ToString());
        }

        private bool IsMenuActivateHovered(Rectangle buttonRect)
        {
            return buttonRect.Contains(Mouse.GetState().Position);
        }

        private void DrawMenuButton(Rectangle rect, string text, bool hovered, float uiScale)
        {
            var fillColor = hovered ? Color.DarkCyan : Color.Black * 0.5f;
            var outlineColor = hovered ? Color.Gold : Color.White;
            _spriteBatch.Draw(_pixelTexture, rect, fillColor);
            DrawRectangleOutline(_spriteBatch, rect, outlineColor, ScaleLength(2, uiScale));

            var textSize = _font.MeasureString(text) * uiScale;
            var textPosition = new Vector2(rect.Center.X - textSize.X / 2f, rect.Center.Y - textSize.Y / 2f);
            _spriteBatch.DrawString(_font, text, textPosition, Color.White, 0f, Vector2.Zero,
                Vector2.One * uiScale, SpriteEffects.None, 0f);
        }

        private void DrawMeleeAttackArea()
        {
            var attackBounds = _weaponManager.MeleeWeapon.GetAttackBounds(
                _player.Transform.Position, _facingDirection);
            var fillColor = _weaponManager.MeleeWeapon.IsReady
                ? Color.Gold * 0.1f
                : Color.White * 0.035f;
            var outlineColor = _weaponManager.MeleeWeapon.IsReady
                ? Color.Gold * 0.45f
                : Color.White * 0.12f;

            _spriteBatch.Draw(_pixelTexture, attackBounds, fillColor);
            DrawRectangleOutline(_spriteBatch, attackBounds, outlineColor);
        }

        private void RemoveObjectsOutsideActiveArea()
        {
            var maxDistanceSquared = DespawnDistance * DespawnDistance;

            for (var i = _entityManager.Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _entityManager.Enemies[i];
                if (!_chunkManager.IsInsideActiveArea(enemy.Transform.Position) ||
                    Vector2.DistanceSquared(enemy.Transform.Position, _player.Transform.Position) > maxDistanceSquared)
                {
                    _entityManager.Enemies.RemoveAt(i);
                }
            }

            for (var i = _entityManager.ExperienceDrops.Count - 1; i >= 0; i--)
            {
                var drop = _entityManager.ExperienceDrops[i];
                if (!_chunkManager.IsInsideActiveArea(drop.Transform.Position) ||
                    Vector2.DistanceSquared(drop.Transform.Position, _player.Transform.Position) > maxDistanceSquared)
                {
                    _entityManager.ExperienceDrops.RemoveAt(i);
                }
            }
        }

        private bool HandlePlayerDamageFeedback()
        {
            if (_combatSystem.PlayerDamageEvents.Count == 0)
                return false;

            _playerHitFlashTimer = PlayerHitFlashDuration;

            foreach (var damageEvent in _combatSystem.PlayerDamageEvents)
            {
                var textPosition = damageEvent.Position + new Vector2(-10f, -45f);
                _floatingTexts.Add(new FloatingText($"-{damageEvent.Damage}", textPosition, Color.Red));
            }

            return true;
        }

        private void UpdateRunMilestoneAchievements()
        {
            if (_survivalTime >= 600f)
                _progressManager.UnlockAchievement("survive_10_min");

            if (_killCount >= 25)
                _progressManager.UnlockAchievement("run_kills_25");

            if (_killCount >= 50)
                _progressManager.UnlockAchievement("run_kills_50");

            if (_player.Experience.Level >= 15)
                _progressManager.UnlockAchievement("level_15_run");
        }

        private void UpdateBranchAchievements()
        {
            if (_skillTreeSystem.IsBranchCompleted(SkillBranch.Sword))
                _progressManager.UnlockAchievement("branch_sword");

            if (_skillTreeSystem.IsBranchCompleted(SkillBranch.Vampire))
                _progressManager.UnlockAchievement("branch_vampire");

            if (_skillTreeSystem.IsBranchCompleted(SkillBranch.Magic))
                _progressManager.UnlockAchievement("branch_magic");

            if (_skillTreeSystem.IsBranchCompleted(SkillBranch.Gunner))
                _progressManager.UnlockAchievement("branch_gunner");

            if (_skillTreeSystem.SelectedBranches.Count == 2)
            {
                var allSelectedBranchesCompleted = true;
                foreach (var branch in _skillTreeSystem.SelectedBranches)
                {
                    if (!_skillTreeSystem.IsBranchCompleted(branch))
                    {
                        allSelectedBranchesCompleted = false;
                        break;
                    }
                }

                if (allSelectedBranchesCompleted)
                    _progressManager.UnlockAchievement("two_branches");
            }
        }

        private void UpdateFloatingTexts(float dt)
        {
            for (var i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                _floatingTexts[i].Update(dt);
                if (!_floatingTexts[i].IsActive)
                    _floatingTexts.RemoveAt(i);
            }
        }

        private bool IsKeyPressed(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
        }

        private int GetDifficultyLevel()
        {
            return 1 + (int)(_survivalTime / 30f);
        }

        private float GetCurrentSpawnInterval()
        {
            var difficultyLevel = GetDifficultyLevel();
            return MathHelper.Max(MinSpawnInterval, BaseSpawnInterval - (difficultyLevel - 1) * 0.08f);
        }

        private int GetCurrentMaxEnemies()
        {
            return BaseMaxEnemies + (GetDifficultyLevel() - 1) * 4;
        }

        private void RedistributeOvercrowdedEnemies()
        {
            if (_entityManager.Enemies.Count <= EnemyCrowdingThreshold)
            {
                ClearCrowdedEnemyGroups();
                return;
            }

            ClearCrowdedEnemyGroups();
            _enemyFactory.PopulateStandardSpawnPositions(
                _player.Transform.Position,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                _crowdingSpawnPositions);

            foreach (var enemy in _entityManager.Enemies)
            {
                if (!enemy.Health.IsAlive)
                    continue;

                var crowdCell = GetCrowdingCell(enemy.Transform.Position);
                if (!_crowdedEnemyGroups.TryGetValue(crowdCell, out var group))
                {
                    group = new List<Enemy>();
                    _crowdedEnemyGroups[crowdCell] = group;
                }

                if (group.Count == 0)
                    _activeCrowdingCells.Add(crowdCell);

                group.Add(enemy);
            }

            for (var groupIndex = 0; groupIndex < _activeCrowdingCells.Count; groupIndex++)
            {
                var group = _crowdedEnemyGroups[_activeCrowdingCells[groupIndex]];
                if (group.Count <= EnemyCrowdingThreshold)
                    continue;

                for (var i = EnemyCrowdingThreshold; i < group.Count; i++)
                {
                    var enemy = group[i];
                    var spawnIndex = (i - EnemyCrowdingThreshold) % _crowdingSpawnPositions.Length;
                    var ring = (i - EnemyCrowdingThreshold) / _crowdingSpawnPositions.Length;
                    enemy.Transform.Position = _crowdingSpawnPositions[spawnIndex] + GetSpawnScatterOffset(spawnIndex, ring);
                }
            }
        }

        private void ClearCrowdedEnemyGroups()
        {
            for (var i = 0; i < _activeCrowdingCells.Count; i++)
            {
                _crowdedEnemyGroups[_activeCrowdingCells[i]].Clear();
            }

            _activeCrowdingCells.Clear();
        }

        private Point GetCrowdingCell(Vector2 position)
        {
            return new Point(
                (int)Math.Floor(position.X / EnemyCrowdingCellSize),
                (int)Math.Floor(position.Y / EnemyCrowdingCellSize));
        }

        private Vector2 GetSpawnScatterOffset(int spawnIndex, int ring)
        {
            var distance = EnemyScatterStep * (ring + 1);

            return spawnIndex switch
            {
                0 => new Vector2(0f, -distance),
                1 => new Vector2(distance, 0f),
                2 => new Vector2(0f, distance),
                _ => new Vector2(-distance, 0f),
            };
        }

        private SkillTreeSystem CreateSkillTreeSystem()
        {
            return new SkillTreeSystem(_skillDefinitionsPath);
        }

        private string FormatTime(float seconds)
        {
            var totalSeconds = (int)seconds;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void DrawScreenBars(float uiScale)
        {
            DrawScreenBar(ScaleRect(new Rectangle(10, 10, 260, 20), uiScale),
                (float)_player.Health.CurrentHp / _player.Health.MaxHp, Color.DarkRed, Color.LimeGreen, uiScale);
            DrawScreenBar(ScaleRect(new Rectangle(160, (int)BaseUiHeight - 28,
                (int)BaseUiWidth - 320, 14), uiScale),
                _player.Experience.GetExperiencePercent(), Color.Black, Color.DeepSkyBlue, uiScale);

            if (_font != null)
            {
                _spriteBatch.DrawString(_font, $"ХП {_player.Health.CurrentHp}/{_player.Health.MaxHp}",
                    ScaleVector(new Vector2(18, 10), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_font,
                    $"ОПЫТ {_player.Experience.CurrentExperience}/{_player.Experience.ExperienceToNextLevel}",
                    ScaleVector(new Vector2(560, 689), uiScale), Color.White, 0f,
                    Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawScreenBar(Rectangle rect, float percent, Color backgroundColor, Color fillColor,
            float uiScale)
        {
            percent = MathHelper.Clamp(percent, 0f, 1f);
            _spriteBatch.Draw(_pixelTexture, rect, backgroundColor);
            _spriteBatch.Draw(_pixelTexture,
                new Rectangle(rect.X, rect.Y, (int)(rect.Width * percent), rect.Height), fillColor);
            DrawRectangleOutline(_spriteBatch, rect, Color.White, Math.Max(1, (int)Math.Round(uiScale)));
        }

        private void DrawGameOverPanel(float uiScale)
        {
            var screenWidth = GraphicsDevice.Viewport.Width;
            var screenHeight = GraphicsDevice.Viewport.Height;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.55f);

            var panelWidth = ScaleLength(360, uiScale);
            var panelHeight = ScaleLength(230, uiScale);
            var panelRect = new Rectangle((screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2, panelWidth, panelHeight);

            _spriteBatch.Draw(_pixelTexture, panelRect, Color.DarkSlateGray);
            DrawRectangleOutline(_spriteBatch, panelRect, Color.White, ScaleLength(1, uiScale));

            var titleSize = _font.MeasureString("ИГРА ОКОНЧЕНА") * uiScale;
            var titlePosition = new Vector2(panelRect.Center.X - titleSize.X / 2f,
                panelRect.Y + ScaleLength(24, uiScale));
            _spriteBatch.DrawString(_font, "ИГРА ОКОНЧЕНА", titlePosition, Color.Red, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);

            var x = panelRect.X + ScaleLength(34, uiScale);
            var y = panelRect.Y + ScaleLength(24, uiScale);
            _spriteBatch.DrawString(_font, $"Продержался: {FormatTime(_survivalTime)}",
                new Vector2(x, y + ScaleLength(48, uiScale)), Color.White, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, $"Убито: {_killCount}",
                new Vector2(x, y + ScaleLength(78, uiScale)), Color.White, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, $"Достигнут уровень: {_player.Experience.Level}",
                new Vector2(x, y + ScaleLength(108, uiScale)), Color.White, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, "R - Заново    Esc - Главное меню",
                new Vector2(x, y + ScaleLength(158, uiScale)), Color.LightGray, 0f,
                Vector2.Zero, Vector2.One * uiScale, SpriteEffects.None, 0f);
        }

        private void ToggleFullscreenWindowed()
        {
            SetFullscreenWindowed(!_graphics.IsFullScreen);
        }

        private void SetFullscreenWindowed(bool isFullscreen)
        {
            if (_graphics.IsFullScreen == isFullscreen)
                return;

            _isHandlingResize = true;

            if (isFullscreen)
            {
                var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                _graphics.PreferredBackBufferWidth = displayMode.Width;
                _graphics.PreferredBackBufferHeight = displayMode.Height;
                _graphics.IsFullScreen = true;
            }
            else
            {
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = WindowedBackBufferWidth;
                _graphics.PreferredBackBufferHeight = WindowedBackBufferHeight;
            }

            _graphics.ApplyChanges();
            UpdateViewportDependentState();
            _isHandlingResize = false;
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (_isHandlingResize || GraphicsDevice == null)
                return;

            UpdateViewportDependentState();
        }

        private void UpdateViewportDependentState()
        {
            if (GraphicsDevice == null || _camera == null)
                return;

            _camera.UpdateViewportSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }

        private float GetUiScale()
        {
            var widthScale = GraphicsDevice.Viewport.Width / BaseUiWidth;
            var heightScale = GraphicsDevice.Viewport.Height / BaseUiHeight;
            return Math.Min(widthScale, heightScale);
        }

        private int ScaleLength(float value, float uiScale)
        {
            return Math.Max(1, (int)Math.Round(value * uiScale));
        }

        private Rectangle ScaleRect(Rectangle rect, float uiScale)
        {
            return new Rectangle(
                ScaleLength(rect.X, uiScale),
                ScaleLength(rect.Y, uiScale),
                ScaleLength(rect.Width, uiScale),
                ScaleLength(rect.Height, uiScale));
        }

        private Vector2 ScaleVector(Vector2 vector, float uiScale)
        {
            return vector * uiScale;
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Vector2 position, int currentHp, int maxHp,
            float uiScale, float verticalOffset)
        {
            var width = ScaleLength(40, uiScale);
            var height = ScaleLength(4, uiScale);
            var offsetPosition = position + new Vector2(0f, -verticalOffset * uiScale);

            var backgroundRect = new Rectangle((int)Math.Round(offsetPosition.X - width / 2f),
                (int)Math.Round(offsetPosition.Y - height / 2f), width, height);
            spriteBatch.Draw(_pixelTexture, backgroundRect, Color.Black);

            var healthPercent = (float)currentHp / maxHp;
            var healthWidth = Math.Max(1, (int)Math.Round(width * healthPercent));
            var healthRect = new Rectangle((int)Math.Round(offsetPosition.X - width / 2f),
                (int)Math.Round(offsetPosition.Y - height / 2f), healthWidth, height);

            Color healthColor = healthPercent > 0.5f ? Color.Green :
                                healthPercent > 0.25f ? Color.Yellow : Color.Red;

            spriteBatch.Draw(_pixelTexture, healthRect, healthColor);
            DrawRectangleOutline(spriteBatch, backgroundRect, Color.White, Math.Max(1, (int)Math.Round(uiScale)));
        }


        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            DrawRectangleOutline(spriteBatch, rect, color, 1);
        }

        private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
