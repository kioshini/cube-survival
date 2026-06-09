using Project1.Entities;
using Project1.Managers;
using System;

namespace Project1.Systems
{
    public sealed class SkillNode
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public SkillBranch Branch { get; }
        public int Cost { get; }
        public string RequiredSkillId { get; }
        public bool IsPurchased { get; private set; }

        private readonly Action<Player, WeaponManager> _apply;

        public SkillNode(string id, string name, string description, SkillBranch branch, int cost,
            Action<Player, WeaponManager> apply, string requiredSkillId = "")
        {
            Id = id;
            Name = name;
            Description = description;
            Branch = branch;
            Cost = cost;
            RequiredSkillId = requiredSkillId;
            _apply = apply;
        }

        public void Purchase(Player player, WeaponManager weaponManager)
        {
            if (IsPurchased)
                return;

            _apply(player, weaponManager);
            IsPurchased = true;
        }
    }
}
