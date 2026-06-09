using Project1.Entities;
using Project1.Managers;
using Project1.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Text;

namespace Project1.Core.UI
{
    public sealed class SkillTreeUI
    {
        private readonly Texture2D _pixelTexture;
        private KeyboardState _previousKeyboardState;
        private int _selectedBranchIndex;
        private int _selectedNodeIndex;
        private bool _isActive;
        private readonly List<string> _wrappedLines = new(4);
        private readonly StringBuilder _wrapLineBuilder = new();
        private readonly StringBuilder _wrapMeasureBuilder = new();

        private static readonly SkillBranch[] BranchOrder =
        {
            SkillBranch.Sword,
            SkillBranch.Vampire,
            SkillBranch.Magic,
            SkillBranch.Gunner
        };

        public bool IsActive => _isActive;

        public SkillTreeUI(Texture2D pixelTexture)
        {
            _pixelTexture = pixelTexture;
            _selectedBranchIndex = 0;
            _selectedNodeIndex = 0;
        }

        public void Show()
        {
            _isActive = true;
            _previousKeyboardState = Keyboard.GetState();
        }

        public void Hide()
        {
            _isActive = false;
        }

        public void Update(KeyboardState keyboardState, SkillTreeSystem skillTree,
            Player player, WeaponManager weaponManager)
        {
            if (!_isActive)
                return;

            if (IsKeyPressed(keyboardState, Keys.A) || IsKeyPressed(keyboardState, Keys.Left))
            {
                _selectedBranchIndex = (_selectedBranchIndex - 1 + BranchOrder.Length) % BranchOrder.Length;
                _selectedNodeIndex = 0;
            }
            if (IsKeyPressed(keyboardState, Keys.D) || IsKeyPressed(keyboardState, Keys.Right))
            {
                _selectedBranchIndex = (_selectedBranchIndex + 1) % BranchOrder.Length;
                _selectedNodeIndex = 0;
            }
            if (IsKeyPressed(keyboardState, Keys.W) || IsKeyPressed(keyboardState, Keys.Up))
            {
                var nodes = skillTree.GetNodes(BranchOrder[_selectedBranchIndex]);
                var nodeCount = nodes.Count;
                _selectedNodeIndex = (_selectedNodeIndex - 1 + nodeCount) % nodeCount;
            }
            if (IsKeyPressed(keyboardState, Keys.S) || IsKeyPressed(keyboardState, Keys.Down))
            {
                var nodes = skillTree.GetNodes(BranchOrder[_selectedBranchIndex]);
                var nodeCount = nodes.Count;
                _selectedNodeIndex = (_selectedNodeIndex + 1) % nodeCount;
            }

            if (IsKeyPressed(keyboardState, Keys.Enter) || IsKeyPressed(keyboardState, Keys.Space))
            {
                var branch = BranchOrder[_selectedBranchIndex];
                var nodes = skillTree.GetNodes(branch);
                skillTree.TryPurchase(nodes[_selectedNodeIndex], player, weaponManager);
            }

            _previousKeyboardState = keyboardState;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, int screenWidth, int screenHeight,
            SkillTreeSystem skillTree, Player player)
        {
            Draw(spriteBatch, font, screenWidth, screenHeight, 1f, skillTree, player);
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, int screenWidth, int screenHeight,
            float uiScale, SkillTreeSystem skillTree, Player player)
        {
            if (!_isActive)
                return;

            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.72f);

            var panelWidth = ScaleLength(1120, uiScale);
            var panelHeight = ScaleLength(600, uiScale);
            var panelRect = new Rectangle((screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2, panelWidth, panelHeight);

            spriteBatch.Draw(_pixelTexture, panelRect, Color.DarkSlateGray);
            DrawRectangleOutline(spriteBatch, panelRect, Color.White, ScaleLength(2, uiScale));

            if (font == null)
                return;

            var textScale = uiScale;
            spriteBatch.DrawString(font, "ДЕРЕВО НАВЫКОВ",
                new Vector2(panelRect.X + ScaleLength(24, uiScale), panelRect.Y + ScaleLength(18, uiScale)),
                Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, $"Очки навыков: {player.Experience.SkillPoints}",
                new Vector2(panelRect.Right - ScaleLength(190, uiScale), panelRect.Y + ScaleLength(18, uiScale)),
                Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, "A/D - Ветвь   W/S - Навык   Enter - Купить   Tab - Закрыть",
                new Vector2(panelRect.X + ScaleLength(300, uiScale), panelRect.Bottom - ScaleLength(38, uiScale)),
                Color.LightGray, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

            var columnWidth = ScaleLength(255, uiScale);
            var nodeHeight = ScaleLength(125, uiScale);
            var gap = ScaleLength(20, uiScale);
            var nodeGap = ScaleLength(14, uiScale);
            var startX = panelRect.X + ScaleLength(35, uiScale);
            var y = panelRect.Y + ScaleLength(82, uiScale);

            for (var i = 0; i < BranchOrder.Length; i++)
            {
                var branch = BranchOrder[i];
                var nodes = skillTree.GetNodes(branch);
                var columnX = startX + i * (columnWidth + gap);
                var branchLocked = skillTree.IsBranchLocked(branch);
                var branchColor = branchLocked ? Color.Gray : Color.White;

                spriteBatch.DrawString(font, GetBranchName(branch),
                    new Vector2(columnX + ScaleLength(8, uiScale), y), branchColor,
                    0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                for (var j = 0; j < nodes.Count; j++)
                {
                    var node = nodes[j];
                    var rect = new Rectangle(columnX, y + 38 + j * (nodeHeight + nodeGap),
                        columnWidth, nodeHeight);
                    var selected = i == _selectedBranchIndex && j == _selectedNodeIndex;
                    var locked = branchLocked && !node.IsPurchased;
                    var requirementMet = skillTree.HasRequirement(node);
                    var canPurchase = skillTree.CanPurchase(node, player);

                    var background = locked || !requirementMet ? Color.DimGray :
                        node.IsPurchased ? Color.DarkOliveGreen :
                        selected ? Color.DarkCyan : Color.Black * 0.45f;

                    spriteBatch.Draw(_pixelTexture, rect, background);
                    DrawRectangleOutline(spriteBatch, rect,
                        selected ? Color.Gold : node.IsPurchased ? Color.LimeGreen : Color.Gray,
                        selected ? ScaleLength(3, uiScale) : ScaleLength(1, uiScale));

                    var textColor = locked || !requirementMet ? Color.Gray : Color.White;
                    spriteBatch.DrawString(font, node.Name,
                        new Vector2(rect.X + ScaleLength(10, uiScale), rect.Y + ScaleLength(8, uiScale)),
                        textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    DrawWrappedString(spriteBatch, font, node.Description,
                        new Vector2(rect.X + ScaleLength(10, uiScale), rect.Y + ScaleLength(34, uiScale)),
                        textColor, rect.Width - ScaleLength(20, uiScale), 3, uiScale);

                    var status = GetStatus(node, locked, requirementMet, canPurchase);
                    var statusColor = node.IsPurchased ? Color.LimeGreen :
                        locked || !requirementMet ? Color.IndianRed :
                        canPurchase ? Color.Gold : Color.LightGray;
                    spriteBatch.DrawString(font, status,
                        new Vector2(rect.X + ScaleLength(10, uiScale), rect.Bottom - ScaleLength(24, uiScale)),
                        statusColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
            }
        }

        private bool IsKeyPressed(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
        }

        private string GetBranchName(SkillBranch branch)
        {
            return branch switch
            {
                SkillBranch.Sword => "Мечник",
                SkillBranch.Vampire => "Вампир",
                SkillBranch.Magic => "Маг",
                SkillBranch.Gunner => "Стрелок",
                _ => branch.ToString()
            };
        }

        private string GetStatus(SkillNode node, bool locked, bool requirementMet, bool canPurchase)
        {
            if (node.IsPurchased)
                return "Куплено";
            if (locked)
                return "Заблокировано: максимум 2 ветки";
            if (!requirementMet)
                return "Нужен предыдущий навык";
            if (canPurchase)
                return $"Доступно - цена {node.Cost}";
            return "Нужно очко навыка";
        }

        private void DrawWrappedString(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color color, int maxWidth, int maxLines, float textScale)
        {
            WrapText(font, text, maxWidth, maxLines, textScale);
            for (var i = 0; i < _wrappedLines.Count; i++)
            {
                spriteBatch.DrawString(font, _wrappedLines[i],
                    position + new Vector2(0, ScaleLength(20, textScale) * i),
                    color, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }
        }

        private void WrapText(SpriteFont font, string text, int maxWidth, int maxLines, float textScale)
        {
            _wrappedLines.Clear();
            var scaledMaxWidth = maxWidth / textScale;
            var hasOverflow = false;
            _wrapLineBuilder.Clear();
            _wrapMeasureBuilder.Clear();

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
                _wrapMeasureBuilder.Clear();
                _wrapMeasureBuilder.Append(_wrapLineBuilder);
                if (_wrapMeasureBuilder.Length > 0)
                    _wrapMeasureBuilder.Append(' ');
                _wrapMeasureBuilder.Append(text, wordStart, wordLength);

                if (font.MeasureString(_wrapMeasureBuilder).X <= scaledMaxWidth)
                {
                    if (_wrapLineBuilder.Length > 0)
                        _wrapLineBuilder.Append(' ');
                    _wrapLineBuilder.Append(text, wordStart, wordLength);
                    continue;
                }

                if (_wrapLineBuilder.Length > 0)
                    _wrappedLines.Add(_wrapLineBuilder.ToString());

                if (_wrappedLines.Count == maxLines)
                {
                    hasOverflow = true;
                    break;
                }

                _wrapLineBuilder.Clear();
                _wrapLineBuilder.Append(text, wordStart, wordLength);
            }

            if (_wrappedLines.Count < maxLines && _wrapLineBuilder.Length > 0)
                _wrappedLines.Add(_wrapLineBuilder.ToString());

            if (hasOverflow && _wrappedLines.Count > 0)
            {
                _wrappedLines[^1] = _wrappedLines[^1].TrimEnd('.') + "...";
            }
        }

        private int ScaleLength(float value, float scale)
        {
            return System.Math.Max(1, (int)System.Math.Round(value * scale));
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
