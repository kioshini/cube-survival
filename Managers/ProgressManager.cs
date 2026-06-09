using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Project1.Managers
{
    public sealed class ProgressManager
    {
        private static readonly AchievementDefinition[] AchievementDefinitions =
        {
            new AchievementDefinition(
                id: "survive_10_min",
                name: "10 минут",
                description: "Прожить 10 минут.",
                isUnlocked: stats => stats.BestSurvivalSeconds >= 600f),
            new AchievementDefinition(
                id: "kills_100",
                name: "Сотня убийств",
                description: "100 убийств за все время.",
                isUnlocked: stats => stats.TotalKills >= 100),
            new AchievementDefinition(
                id: "run_kills_25",
                name: "25 за забег",
                description: "25 убийств за забег.",
                isUnlocked: stats => stats.BestRunKills >= 25),
            new AchievementDefinition(
                id: "run_kills_50",
                name: "50 за забег",
                description: "50 убийств за забег.",
                isUnlocked: stats => stats.BestRunKills >= 50),
            new AchievementDefinition(
                id: "no_damage_5min",
                name: "Без урона",
                description: "5 минут без урона.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "two_branches",
                name: "Две ветки",
                description: "Пройти 2 ветки.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "branch_sword",
                name: "Ветка мечника",
                description: "Пройти ветку мечника.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "branch_vampire",
                name: "Ветка вампира",
                description: "Пройти ветку вампира.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "branch_magic",
                name: "Ветка мага",
                description: "Пройти ветку мага.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "branch_gunner",
                name: "Ветка стрелка",
                description: "Пройти ветку стрелка.",
                isUnlocked: _ => false),
            new AchievementDefinition(
                id: "level_15_run",
                name: "Уровень 15",
                description: "Достичь 15 уровня.",
                isUnlocked: stats => stats.HighestLevelReached >= 15),
            new AchievementDefinition(
                id: "vampire_heal_50",
                name: "Кровосос",
                description: "50 ХП вампиром.",
                isUnlocked: _ => false),
        };
        private static readonly HashSet<string> AchievementIds = CreateAchievementIdSet();

        private readonly string _savePath;
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };
        private readonly HashSet<string> _unlockedAchievementIds = new(StringComparer.Ordinal);
        private readonly List<AchievementView> _achievementViews = new(AchievementDefinitions.Length);

        public GameProgressData Progress { get; private set; }

        public ProgressManager()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Project1");

            _savePath = Path.Combine(appDataFolder, "progress.json");
            Progress = Load();
            SyncAchievementState();
            RebuildAchievementViews();
        }

        public void RecordRun(int kills, float survivalSeconds, int levelReached)
        {
            RecordRunSummary(kills, survivalSeconds, levelReached);
        }

        public void RecordRunSummary(int kills, float survivalSeconds, int levelReached)
        {
            Progress.RunsPlayed++;
            Progress.BestRunKills = Math.Max(Progress.BestRunKills, kills);
            Progress.BestSurvivalSeconds = Math.Max(Progress.BestSurvivalSeconds, survivalSeconds);
            Progress.HighestLevelReached = Math.Max(Progress.HighestLevelReached, levelReached);

            UnlockEligibleAchievements();
            Save();
        }

        public void RecordLifetimeKill()
        {
            Progress.TotalKills++;

            if (Progress.TotalKills >= 100)
                UnlockAchievement("kills_100");

            Save();
        }

        public void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
                return;

            if (_unlockedAchievementIds.Contains(achievementId))
                return;

            if (!AchievementIds.Contains(achievementId))
                return;

            _unlockedAchievementIds.Add(achievementId);
            Progress.UnlockedAchievementIds.Add(achievementId);
            RebuildAchievementViews();
            Save();
        }

        public IReadOnlyList<AchievementView> GetAchievementViews()
        {
            return _achievementViews;
        }

        public string FormatSurvivalTime(float seconds)
        {
            var totalSeconds = (int)Math.Floor(seconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private GameProgressData Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                    return new GameProgressData();

                var json = File.ReadAllText(_savePath);
                var data = JsonSerializer.Deserialize<GameProgressData>(json, _serializerOptions);
                return data ?? new GameProgressData();
            }
            catch
            {
                return new GameProgressData();
            }
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(Progress, _serializerOptions);
                File.WriteAllText(_savePath, json);
            }
            catch
            {
            }
        }

        private void UnlockEligibleAchievements()
        {
            var hasChanges = false;
            foreach (var definition in AchievementDefinitions)
            {
                if (definition.IsUnlocked(Progress) && _unlockedAchievementIds.Add(definition.Id))
                {
                    Progress.UnlockedAchievementIds.Add(definition.Id);
                    hasChanges = true;
                }
            }

            if (hasChanges)
                RebuildAchievementViews();
        }

        private void SyncAchievementState()
        {
            _unlockedAchievementIds.Clear();

            var deduplicatedIds = new List<string>(Progress.UnlockedAchievementIds.Count);
            for (var i = 0; i < Progress.UnlockedAchievementIds.Count; i++)
            {
                var achievementId = Progress.UnlockedAchievementIds[i];
                if (_unlockedAchievementIds.Add(achievementId))
                    deduplicatedIds.Add(achievementId);
            }

            Progress.UnlockedAchievementIds = deduplicatedIds;
        }

        private void RebuildAchievementViews()
        {
            _achievementViews.Clear();
            for (var i = 0; i < AchievementDefinitions.Length; i++)
            {
                var definition = AchievementDefinitions[i];
                _achievementViews.Add(new AchievementView(
                    definition.Name,
                    definition.Description,
                    _unlockedAchievementIds.Contains(definition.Id)));
            }
        }

        private static HashSet<string> CreateAchievementIdSet()
        {
            var achievementIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < AchievementDefinitions.Length; i++)
            {
                achievementIds.Add(AchievementDefinitions[i].Id);
            }

            return achievementIds;
        }
    }

    public sealed class GameProgressData
    {
        public int RunsPlayed { get; set; }
        public int TotalKills { get; set; }
        public int BestRunKills { get; set; }
        public float BestSurvivalSeconds { get; set; }
        public int HighestLevelReached { get; set; }
        public List<string> UnlockedAchievementIds { get; set; } = new();
    }

    public sealed class AchievementDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public Func<GameProgressData, bool> IsUnlocked { get; }

        public AchievementDefinition(string id, string name, string description, Func<GameProgressData, bool> isUnlocked)
        {
            Id = id;
            Name = name;
            Description = description;
            IsUnlocked = isUnlocked;
        }
    }

    public sealed class AchievementView
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsUnlocked { get; }

        public AchievementView(string name, string description, bool isUnlocked)
        {
            Name = name;
            Description = description;
            IsUnlocked = isUnlocked;
        }
    }
}
