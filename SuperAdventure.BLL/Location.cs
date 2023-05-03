using System.Collections.Generic;
using System.Linq;

namespace SuperAdventure.BLL
{
    public class Location
    {
        private readonly SortedList<int, int> _monstersAtLocation = new SortedList<int, int>();

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Item ItemRequiredToEnter { get; set; }
        public Quest QuestAvailableHere { get; set; }
        public Monster MonsterLivingHere { get; set; }
        public Vendor VendorWorkingHere { get; set; }
        public Location LocationToNorth { get; set; }
        public Location LocationToEast { get; set; }
        public Location LocationToSouth { get; set; }
        public Location LocationToWest { get; set; }

        public bool HasAMonster => _monstersAtLocation.Count > 0;

        public bool HasAQuest => QuestAvailableHere != null;
        public bool DoesNotHaveAnItemRequiredToEnter => ItemRequiredToEnter == null;

        public Location(int id, string name, string description,
            Item itemRequiredToEnter = null, Quest questAvailableHere = null, Monster monsterLivingHere = null)
        {
            Id = id;
            Name = name;
            Description = description;
            ItemRequiredToEnter = itemRequiredToEnter;
            QuestAvailableHere = questAvailableHere;
            MonsterLivingHere = monsterLivingHere;
        }

        public void AddMonster(int monsterId, int percentageOfAppearance)
        {
            if (_monstersAtLocation.ContainsKey(monsterId))
            {
                _monstersAtLocation[monsterId] = percentageOfAppearance;
            }
            else
            {
                _monstersAtLocation.Add(monsterId, percentageOfAppearance);
            }
        }

        public Monster NewInstanceOfMonsterLivingHere()
        {
            if (!HasAMonster)
            {
                return null;
            }
            // Total the percentages of all monsters at this location.
            var totalPercentages = _monstersAtLocation.Values.Sum();
            // Select a random number between 1 and the total (in case the total of percentages is not 100).
            var randomNumber = RandomNumberGenerator.NumberBetween(1, totalPercentages);

            // Loop through the monster list, 
            // adding the monster's percentage chance of appearing to the runningTotal variable.
            // When the random number is lower than the runningTotal,
            // that is the monster to return.
            var runningTotal = 0;

            foreach (KeyValuePair<int, int> monsterKeyValuePair in _monstersAtLocation)
            {
                runningTotal += monsterKeyValuePair.Value;
                if (randomNumber <= runningTotal)
                {
                    return World.MonsterById(monsterKeyValuePair.Key).NewInstanceOfMonster();
                }
            }
            // In case there was a problem, return the last monster in the list.
            return World.MonsterById(_monstersAtLocation.Keys.Last()).NewInstanceOfMonster();

        }
    }
}
