using System.Collections.Generic;
using System.Linq;

namespace SuperAdventure.BLL
{
    public class Monster : LivingCreature
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaximumDamage { get; set; }
        public int RewardExperiencePoints { get; set; }
        public int RewardGold { get; set; }

        public List<LootItem> LootTable { get; set; }

        internal List<InventoryItem> LootItems { get; }
        public Monster(int id, string name, int maximumDamage, int rewardExperiencePoints, int rewardGold, int currentHitPoints, int maximumHitPoints)
             : base(currentHitPoints, maximumHitPoints)
        {
            Id = id;
            Name = name;
            MaximumDamage = maximumDamage;
            RewardExperiencePoints = rewardExperiencePoints;
            RewardGold = rewardGold;
            LootTable = new List<LootItem>();
            LootItems = new List<InventoryItem>();
        }

        internal Monster NewInstanceOfMonster()
        {
            var newMonster =
                new Monster(Id, Name, MaximumDamage, RewardExperiencePoints, RewardGold, CurrentHitPoints, MaximumHitPoints);

            // Add items to the lootedItems list, comparing a random number to the drop percentage
            foreach (LootItem lootItem in LootTable.Where(lootItem => RandomNumberGenerator.NumberBetween(1, 100) <= lootItem.DropPercentage))
            {
                newMonster.LootItems.Add(new InventoryItem(lootItem.Details, 1));
            }

            // If no items were randomly selected, add the default loot item(s).
            if (newMonster.LootItems.Count == 0)
            {
                foreach (LootItem lootItem in LootTable.Where(x => x.IsDefaultItem))
                {
                    newMonster.LootItems.Add(new InventoryItem(lootItem.Details, 1));
                }
            }

            return newMonster;
        }
    }
}
