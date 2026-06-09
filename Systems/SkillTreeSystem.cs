using Project1.Entities;
using Project1.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Project1.Systems
{
    public sealed class SkillTreeSystem
    {
        private static readonly SkillNodeDefinitionData[] FallbackDefinitions =
        {
            new SkillNodeDefinitionData
            {
                Id = "sword_path",
                Name = "Путь мечника",
                Description = "Обычный удар становится мощным ударом мечом: урон 3, дальность 75, целей 2.",
                Branch = "Sword",
                Cost = 1,
                EffectId = "apply_sword_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "wide_slash",
                Name = "Широкий взмах",
                Description = "Увеличивает дальность ближнего боя и число целей на 1.",
                Branch = "Sword",
                Cost = 1,
                EffectId = "improve_sword_reach",
                RequiredSkillId = "sword_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "blade_tempo",
                Name = "Темп клинка",
                Description = "Увеличивает урон и скорость атаки ближнего боя.",
                Branch = "Sword",
                Cost = 1,
                EffectId = "improve_sword_tempo",
                RequiredSkillId = "wide_slash"
            },
            new SkillNodeDefinitionData
            {
                Id = "blood_path",
                Name = "Кровавый путь",
                Description = "Урон ближнего боя 3. Лечение 1 ХП при убийстве в ближнем бою.",
                Branch = "Vampire",
                Cost = 1,
                EffectId = "apply_blood_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "life_leech",
                Name = "Похищение жизни",
                Description = "Убийства ближним боем лечат сильнее, а урон растет.",
                Branch = "Vampire",
                Cost = 1,
                EffectId = "improve_blood_leech",
                RequiredSkillId = "blood_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "blood_body",
                Name = "Кровавое тело",
                Description = "Даёт больше максимум ХП и усиливает лечение от убийств.",
                Branch = "Vampire",
                Cost = 1,
                EffectId = "improve_blood_body",
                RequiredSkillId = "life_leech"
            },
            new SkillNodeDefinitionData
            {
                Id = "magic_path",
                Name = "Путь магии",
                Description = "Открывает автоматический арканный снаряд: урон 3, дальность 320.",
                Branch = "Magic",
                Cost = 1,
                EffectId = "unlock_magic_weapon"
            },
            new SkillNodeDefinitionData
            {
                Id = "arcane_power",
                Name = "Сила арканы",
                Description = "Арканный снаряд бьет сильнее и летит дальше.",
                Branch = "Magic",
                Cost = 1,
                EffectId = "improve_magic_power",
                RequiredSkillId = "magic_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "mana_flow",
                Name = "Поток маны",
                Description = "Арканный снаряд стреляет чаще и летит быстрее.",
                Branch = "Magic",
                Cost = 1,
                EffectId = "improve_magic_flow",
                RequiredSkillId = "arcane_power"
            },
            new SkillNodeDefinitionData
            {
                Id = "gunner_path",
                Name = "Путь стрелка",
                Description = "Открывает автоматический пистолет: урон 2, быстрая стрельба, дальность 360.",
                Branch = "Gunner",
                Cost = 1,
                EffectId = "unlock_gun_weapon"
            },
            new SkillNodeDefinitionData
            {
                Id = "quick_reload",
                Name = "Быстрая перезарядка",
                Description = "Пистолет стреляет чаще.",
                Branch = "Gunner",
                Cost = 1,
                EffectId = "improve_gun_reload",
                RequiredSkillId = "gunner_path"
            },
            new SkillNodeDefinitionData
            {
                Id = "heavy_rounds",
                Name = "Тяжелые патроны",
                Description = "Пистолет бьет сильнее и летит быстрее.",
                Branch = "Gunner",
                Cost = 1,
                EffectId = "improve_gun_rounds",
                RequiredSkillId = "quick_reload"
            }
        };

        private readonly List<SkillNode> _nodes = new();
        private readonly Dictionary<SkillBranch, List<SkillNode>> _nodesByBranch = new();
        private readonly Dictionary<string, SkillNode> _nodesById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<Player, WeaponManager>> _skillEffects;
        private readonly HashSet<SkillBranch> _selectedBranches = new();
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private const int MaxSelectedBranches = 2;

        public IReadOnlyList<SkillNode> Nodes => _nodes;
        public IReadOnlyCollection<SkillBranch> SelectedBranches => _selectedBranches;

        public SkillTreeSystem(string skillDefinitionsPath = null)
        {
            _skillEffects = CreateSkillEffects();
            InitializeBranchBuckets();
            LoadNodes(skillDefinitionsPath);
        }

        public IReadOnlyList<SkillNode> GetNodes(SkillBranch branch)
        {
            return _nodesByBranch.TryGetValue(branch, out var nodes)
                ? nodes
                : Array.Empty<SkillNode>();
        }

        public bool CanPurchase(SkillNode node, Player player)
        {
            if (node.IsPurchased)
                return false;
            if (player.Experience.SkillPoints < node.Cost)
                return false;
            if (!HasRequirement(node))
                return false;

            return _selectedBranches.Contains(node.Branch) ||
                   _selectedBranches.Count < MaxSelectedBranches;
        }

        public bool IsBranchLocked(SkillBranch branch)
        {
            return !_selectedBranches.Contains(branch) &&
                   _selectedBranches.Count >= MaxSelectedBranches;
        }

        public bool HasRequirement(SkillNode node)
        {
            if (string.IsNullOrWhiteSpace(node.RequiredSkillId))
                return true;

            return _nodesById.TryGetValue(node.RequiredSkillId, out var requiredNode) &&
                   requiredNode.IsPurchased;
        }

        public bool IsBranchCompleted(SkillBranch branch)
        {
            var nodes = GetNodes(branch);
            if (nodes.Count == 0)
                return false;

            for (var i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].IsPurchased)
                    return false;
            }

            return true;
        }

        public bool TryPurchase(SkillNode node, Player player, WeaponManager weaponManager)
        {
            if (!CanPurchase(node, player))
                return false;
            if (!player.Experience.SpendSkillPoint(node.Cost))
                return false;

            node.Purchase(player, weaponManager);
            _selectedBranches.Add(node.Branch);
            return true;
        }

        private void InitializeBranchBuckets()
        {
            _nodesByBranch[SkillBranch.Sword] = new List<SkillNode>();
            _nodesByBranch[SkillBranch.Vampire] = new List<SkillNode>();
            _nodesByBranch[SkillBranch.Magic] = new List<SkillNode>();
            _nodesByBranch[SkillBranch.Gunner] = new List<SkillNode>();
        }

        private void LoadNodes(string skillDefinitionsPath)
        {
            var definitions = LoadDefinitions(skillDefinitionsPath);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (!TryParseBranch(definition.Branch, out var branch))
                    continue;

                if (string.IsNullOrWhiteSpace(definition.Id) ||
                    string.IsNullOrWhiteSpace(definition.Name) ||
                    string.IsNullOrWhiteSpace(definition.Description) ||
                    string.IsNullOrWhiteSpace(definition.EffectId))
                {
                    continue;
                }

                if (!_skillEffects.TryGetValue(definition.EffectId, out var apply))
                    continue;

                var node = new SkillNode(
                    definition.Id,
                    definition.Name,
                    definition.Description,
                    branch,
                    definition.Cost,
                    apply,
                    definition.RequiredSkillId ?? string.Empty);

                _nodes.Add(node);
                _nodesByBranch[branch].Add(node);
                _nodesById[node.Id] = node;
            }
        }

        private List<SkillNodeDefinitionData> LoadDefinitions(string skillDefinitionsPath)
        {
            if (!string.IsNullOrWhiteSpace(skillDefinitionsPath))
            {
                try
                {
                    if (File.Exists(skillDefinitionsPath))
                    {
                        var json = File.ReadAllText(skillDefinitionsPath);
                        var definitions = JsonSerializer.Deserialize<List<SkillNodeDefinitionData>>(json, _serializerOptions);
                        if (definitions != null && definitions.Count > 0)
                            return definitions;
                    }
                }
                catch
                {
                }
            }

            return new List<SkillNodeDefinitionData>(FallbackDefinitions);
        }

        private static bool TryParseBranch(string branchName, out SkillBranch branch)
        {
            return Enum.TryParse(branchName, ignoreCase: true, out branch);
        }

        private static Dictionary<string, Action<Player, WeaponManager>> CreateSkillEffects()
        {
            return new Dictionary<string, Action<Player, WeaponManager>>(StringComparer.Ordinal)
            {
                ["apply_sword_path"] = (_, weaponManager) => weaponManager.ApplySwordPath(),
                ["improve_sword_reach"] = (_, weaponManager) => weaponManager.ImproveSwordReach(),
                ["improve_sword_tempo"] = (_, weaponManager) => weaponManager.ImproveSwordTempo(),
                ["apply_blood_path"] = (_, weaponManager) => weaponManager.ApplyBloodPath(),
                ["improve_blood_leech"] = (_, weaponManager) => weaponManager.ImproveBloodLeech(),
                ["improve_blood_body"] = (player, weaponManager) => weaponManager.ImproveBloodBody(player),
                ["unlock_magic_weapon"] = (_, weaponManager) => weaponManager.UnlockMagicWeapon(),
                ["improve_magic_power"] = (_, weaponManager) => weaponManager.ImproveMagicPower(),
                ["improve_magic_flow"] = (_, weaponManager) => weaponManager.ImproveMagicFlow(),
                ["unlock_gun_weapon"] = (_, weaponManager) => weaponManager.UnlockGunWeapon(),
                ["improve_gun_reload"] = (_, weaponManager) => weaponManager.ImproveGunReload(),
                ["improve_gun_rounds"] = (_, weaponManager) => weaponManager.ImproveGunRounds(),
            };
        }

        private sealed class SkillNodeDefinitionData
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Branch { get; set; }
            public int Cost { get; set; }
            public string EffectId { get; set; }
            public string RequiredSkillId { get; set; }
        }
    }
}
