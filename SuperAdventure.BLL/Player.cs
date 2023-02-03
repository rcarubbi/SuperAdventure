using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;

namespace SuperAdventure.BLL
{
    public class Player : LivingCreature
    {
        private int _gold;
        private int _experiencePoints;
        private Location _currentLocation;

        public event EventHandler<MessageEventArgs> OnMessage;
        public int Gold
        {
            get => _gold;
            set
            {
                _gold = value;
                OnPropertyChanged("Gold");
            }
        }
        public int ExperiencePoints
        {
            get => _experiencePoints;
            private set
            {
                _experiencePoints = value;
                OnPropertyChanged("ExperiencePoints");
                OnPropertyChanged("Level");
            }
        }
        public int Level => (ExperiencePoints / 100) + 1;
        public Location CurrentLocation
        {
            get => _currentLocation;
            set
            {
                _currentLocation = value;
                OnPropertyChanged("CurrentLocation");
            }
        }
        public Weapon CurrentWeapon { get; set; }
        public BindingList<InventoryItem> Inventory { get; set; }
        public List<Weapon> Weapons => Inventory.Where(x => x.Details is Weapon).Select(x => x.Details as Weapon).ToList();
        public List<HealingPotion> Potions => Inventory.Where(x => x.Details is HealingPotion).Select(x => x.Details as HealingPotion).ToList();
        public BindingList<PlayerQuest> Quests { get; set; }
        public Monster CurrentMonster { get; set; }
        public Player(int currentHitPoints, int maximumHitPoints, int gold, int experiencePoints)
          : base(currentHitPoints, maximumHitPoints)
        {
            Gold = gold;
            ExperiencePoints = experiencePoints;
            Inventory = new BindingList<InventoryItem>();
            Quests = new BindingList<PlayerQuest>();
        }
        public static Player CreateDefaultPlayer()
        {
            Player player = new Player(10, 10, 20, 0);
            player.Inventory.Add(new InventoryItem(World.ItemById(World.ITEM_ID_RUSTY_SWORD), 1));
            player.CurrentLocation = World.LocationById(World.LOCATION_ID_HOME);
            return player;
        }
        public static Player CreatePlayerFromXmlString(string xmlPlayerData)
        {
            try
            {
                XmlDocument playerData = new XmlDocument();
                playerData.LoadXml(xmlPlayerData);
                int currentHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentHitPoints").InnerText);
                int maximumHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/MaximumHitPoints").InnerText);
                int gold = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/Gold").InnerText);
                int experiencePoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/ExperiencePoints").InnerText);
                Player player = new Player(currentHitPoints, maximumHitPoints, gold, experiencePoints);
                int currentLocationID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentLocation").InnerText);
                player.CurrentLocation = World.LocationById(currentLocationID);

                if (playerData.SelectSingleNode("/Player/Stats/CurrentWeapon") != null)
                {
                    int currentWeaponID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentWeapon").InnerText);
                    player.CurrentWeapon = (Weapon)World.ItemById(currentWeaponID);
                }


                foreach (XmlNode node in playerData.SelectNodes("/Player/InventoryItems/InventoryItem"))
                {
                    int id = Convert.ToInt32(node.Attributes["Id"].Value);
                    int quantity = Convert.ToInt32(node.Attributes["Quantity"].Value);
                    player.AddItemToInventory(World.ItemById(id), quantity);
                }
                foreach (XmlNode node in playerData.SelectNodes("/Player/PlayerQuests/PlayerQuest"))
                {
                    int id = Convert.ToInt32(node.Attributes["Id"].Value);
                    bool isCompleted = Convert.ToBoolean(node.Attributes["IsCompleted"].Value);
                    PlayerQuest playerQuest = new PlayerQuest(World.QuestById(id))
                    {
                        IsCompleted = isCompleted
                    };
                    player.Quests.Add(playerQuest);
                }
                return player;
            }
            catch
            {
                // If there was an error with the XML data, return a default player object
                return CreateDefaultPlayer();
            }
        }
        public static Player CreatePlayerFromDatabase(int currentHitPoints, int maximumHitPoints, int gold, int experiencePoints, int currentLocationId)
        {
            Player player = new Player(currentHitPoints, maximumHitPoints, gold, experiencePoints);
            player.MoveTo(World.LocationById(currentLocationId));
            return player;
        }
        public void MoveTo(Location location)
        {
            if (PlayerDoesNotHaveTheRequiredItemToEnter(location))
            {
                RaiseMessage("You must have a " + location.ItemRequiredToEnter.Name + " to enter this location.");
                return;
            }

            // The player can enter this location
            CurrentLocation = location;
            CompletelyHeal();

            if (location.HasAQuest)
            {
                if (PlayerDoesNotHaveThisQuest(location.QuestAvailableHere))
                {
                    GiveQuestToPlayer(location.QuestAvailableHere);
                }
                else
                {
                    if (PlayerHasNotCompleted(location.QuestAvailableHere)
                        && PlayerHasAllQuestCompletionItemsFor(location.QuestAvailableHere))
                    {
                        GivePlayerQuestRewards(location);
                    }
                }
            }

            SetTheCurrentMonsterForTheCurrentLocation(location);
        }
        public void MoveNorth()
        {
            if (CurrentLocation.LocationToNorth != null)
            {
                MoveTo(CurrentLocation.LocationToNorth);
            }
        }
        public void MoveEast()
        {
            if (CurrentLocation.LocationToEast != null)
            {
                MoveTo(CurrentLocation.LocationToEast);
            }
        }
        public void MoveSouth()
        {
            if (CurrentLocation.LocationToSouth != null)
            {
                MoveTo(CurrentLocation.LocationToSouth);
            }
        }
        public void MoveWest()
        {
            if (CurrentLocation.LocationToWest != null)
            {
                MoveTo(CurrentLocation.LocationToWest);
            }
        }
        public void UseWeapon(Weapon currentWeapon)
        {

            int damage = RandomNumberGenerator.NumberBetween(currentWeapon.MinimumDamage, currentWeapon.MaximumDamage);

            if (damage == 0)
            {
                RaiseMessage("You missed the " + CurrentMonster.Name);
            }
            else
            {
                CurrentMonster.CurrentHitPoints -= damage;
                RaiseMessage("You hit the " + CurrentMonster.Name + " for " + damage + " points.");
            }

            if (CurrentMonster.IsDead)
            {
                LootTheCurrentMonster();

                // "Move" to the current location, to refresh the current monster
                MoveTo(CurrentLocation);
            }
            else
            {
                LetTheMonsterAttack();
            }
        }
        public void UsePotion(HealingPotion potion)
        {
            RaiseMessage($"You drink a {potion.Name}");

            HealPlayer(potion.AmountToHeal);

            RemoveItemFromInventory(potion);

            LetTheMonsterAttack();
        }
        public void AddItemToInventory(Item itemToAdd, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.Id == itemToAdd.Id);
            if (item == null)
            {
                // They didn't have the item, so add it to their inventory
                Inventory.Add(new InventoryItem(itemToAdd, quantity));
            }
            else
            {
                // They have the item in their inventory, so increase the quantity  
                item.Quantity += quantity;
            }
            RaiseInventoryChangedEvent(itemToAdd);
        }
        public void RemoveItemFromInventory(Item itemToRemove, int quantity = 1)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.Id == itemToRemove.Id);
            if (item != null)
            {
                // They have the item in their inventory, so decrease the quantity
                item.Quantity -= quantity;
                // Don't allow negative quantities.
                // We might want to raise an error for this situation
                if (item.Quantity < 0)
                {
                    item.Quantity = 0;
                }
                // If the quantity is zero, remove the item from the list
                if (item.Quantity == 0)
                {
                    Inventory.Remove(item);
                }
                // Notify the UI that the inventory has changed
                RaiseInventoryChangedEvent(itemToRemove);
            }
        }
        public string ToXmlString()
        {
            XmlDocument playerData = new XmlDocument();

            // Create the top-level XML node
            XmlNode player = playerData.CreateElement("Player");
            playerData.AppendChild(player);

            // Create the "Stats" child node to hold the other player statistics nodes
            XmlNode stats = playerData.CreateElement("Stats");
            player.AppendChild(stats);

            // Create the child nodes for the "Stats" node
            CreateNewChildXmlNode(playerData, stats, "CurrentHitPoints", CurrentHitPoints);
            CreateNewChildXmlNode(playerData, stats, "MaximumHitPoints", MaximumHitPoints);
            CreateNewChildXmlNode(playerData, stats, "Gold", Gold);
            CreateNewChildXmlNode(playerData, stats, "ExperiencePoints", ExperiencePoints);
            CreateNewChildXmlNode(playerData, stats, "CurrentLocation", CurrentLocation.Id);

            if (CurrentWeapon != null)
            {
                CreateNewChildXmlNode(playerData, stats, "CurrentWeapon", CurrentWeapon.Id);
            }

            // Create the "InventoryItems" child node to hold each InventoryItem node
            XmlNode inventoryItems = playerData.CreateElement("InventoryItems");
            player.AppendChild(inventoryItems);

            // Create an "InventoryItem" node for each item in the player's inventory
            foreach (InventoryItem item in Inventory)
            {
                XmlNode inventoryItem = playerData.CreateElement("InventoryItem");

                AddXmlAttributeToNode(playerData, inventoryItem, "Id", item.Details.Id);
                AddXmlAttributeToNode(playerData, inventoryItem, "Quantity", item.Quantity);

                inventoryItems.AppendChild(inventoryItem);
            }

            // Create the "PlayerQuests" child node to hold each PlayerQuest node
            XmlNode playerQuests = playerData.CreateElement("PlayerQuests");
            player.AppendChild(playerQuests);

            // Create a "PlayerQuest" node for each quest the player has acquired
            foreach (PlayerQuest quest in Quests)
            {
                XmlNode playerQuest = playerData.CreateElement("PlayerQuest");

                AddXmlAttributeToNode(playerData, playerQuest, "Id", quest.Details.Id);
                AddXmlAttributeToNode(playerData, playerQuest, "IsCompleted", quest.IsCompleted);

                playerQuests.AppendChild(playerQuest);
            }

            return playerData.InnerXml; // The XML document, as a string, so we can save the data to disk
        }
        public bool HasRequiredItemToEnterThisLocation(Location location)
        {
            if (location.ItemRequiredToEnter == null)
            {
                // There is no required item for this location, so return "true"
                return true;
            }
            // See if the player has the required item in their inventory
            return Inventory.Any(ii => ii.Details.Id == location.ItemRequiredToEnter.Id);
        }
        public bool PlayerDoesNotHaveThisQuest(Quest quest) => Quests.All(pq => pq.Details.Id != quest.Id);
        public void RemoveQuestCompletionItems(Quest quest)
        {
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.Id == qci.Details.Id);
                if (item != null)
                {
                    // Subtract the quantity from the player's inventory that was needed to complete the quest
                    RemoveItemFromInventory(item.Details, qci.Quantity);
                }
            }
        }
        public void AddExperiencePoints(int experiencePointsToAdd)
        {
            ExperiencePoints += experiencePointsToAdd;
            MaximumHitPoints = (Level * 10);
        }
        public void MarkPlayerQuestCompleted(Quest quest)
        {

            PlayerQuest playerQuest = Quests.SingleOrDefault(pq => pq.Details.Id == quest.Id);
            if (playerQuest != null)
            {
                playerQuest.IsCompleted = true;
            }
        }
        private void LootTheCurrentMonster()
        {
            RaiseMessage("");
            RaiseMessage("You defeated the " + CurrentMonster.Name);
            RaiseMessage("You receive " + CurrentMonster.RewardExperiencePoints + " experience points");
            RaiseMessage("You receive " + CurrentMonster.RewardGold + " gold");

            AddExperiencePoints(CurrentMonster.RewardExperiencePoints);
            Gold += CurrentMonster.RewardGold;

            // Give monster's loot items to the player
            foreach (InventoryItem inventoryItem in CurrentMonster.LootItems)
            {
                AddItemToInventory(inventoryItem.Details);
                RaiseMessage(string.Format("You loot {0} {1}", inventoryItem.Quantity, inventoryItem.Description));
            }

            RaiseMessage(string.Empty);
        }
        private void SetTheCurrentMonsterForTheCurrentLocation(Location location)
        {
            // Populate the current monster with this location's monster (or null, if there is no monster here)
            CurrentMonster = location.NewInstanceOfMonsterLivingHere();

            if (CurrentMonster != null)
            {
                RaiseMessage("You see a " + CurrentMonster.Name);
            }
        }
        private bool PlayerDoesNotHaveTheRequiredItemToEnter(Location location) => !HasRequiredItemToEnterThisLocation(location);
        private bool PlayerHasNotCompleted(Quest quest) => Quests.Any(pq => pq.Details.Id == quest.Id && !pq.IsCompleted);
        private void GiveQuestToPlayer(Quest quest)
        {
            RaiseMessage("You receive the " + quest.Name + " quest.");
            RaiseMessage(quest.Description);
            RaiseMessage("To complete it, return with:");

            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                RaiseMessage(string.Format("{0} {1}", qci.Quantity,
                    qci.Quantity == 1 ? qci.Details.Name : qci.Details.NamePlural));
            }

            Quests.Add(new PlayerQuest(quest));
        }
        private bool PlayerHasAllQuestCompletionItemsFor(Quest quest)
        {
            // See if the player has all the items needed to complete the quest here
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                // Check each item in the player's inventory, to see if they have it, and enough of it
                if (!Inventory.Any(ii => ii.Details.Id == qci.Details.Id && ii.Quantity >= qci.Quantity))
                {
                    return false;
                }
            }

            // If we got here, then the player must have all the required items, and enough of them, to complete the quest.
            return true;
        }
        private void GivePlayerQuestRewards(Location newLocation)
        {
            RaiseMessage("");
            RaiseMessage("You complete the '" + newLocation.QuestAvailableHere.Name + "' quest.");
            RaiseMessage("You receive: ");
            RaiseMessage(newLocation.QuestAvailableHere.RewardExperiencePoints + " experience points");
            RaiseMessage(newLocation.QuestAvailableHere.RewardGold + " gold");
            RaiseMessage(newLocation.QuestAvailableHere.RewardItem.Name, true);

            AddExperiencePoints(newLocation.QuestAvailableHere.RewardExperiencePoints);
            Gold += newLocation.QuestAvailableHere.RewardGold;

            RemoveQuestCompletionItems(newLocation.QuestAvailableHere);
            AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);

            MarkPlayerQuestCompleted(newLocation.QuestAvailableHere);
        }
        private void LetTheMonsterAttack()
        {
            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, CurrentMonster.MaximumDamage);

            RaiseMessage($"The {CurrentMonster.Name} did {damageToPlayer} points of damage.");

            CurrentHitPoints -= damageToPlayer;

            if (IsDead)
            {
                RaiseMessage($"The {CurrentMonster.Name} killed you.");
                MoveHome();
            }
        }
        private void HealPlayer(int hitPointsToHeal) => CurrentHitPoints = Math.Min(CurrentHitPoints + hitPointsToHeal, MaximumHitPoints);
        private void CompletelyHeal() => CurrentHitPoints = MaximumHitPoints;
        private void MoveHome() => MoveTo(World.LocationById(World.LOCATION_ID_HOME));
        private void CreateNewChildXmlNode(XmlDocument document, XmlNode parentNode, string elementName, object value)
        {
            XmlNode node = document.CreateElement(elementName);
            node.AppendChild(document.CreateTextNode(value.ToString()));
            parentNode.AppendChild(node);
        }
        private void AddXmlAttributeToNode(XmlDocument document, XmlNode node, string attributeName, object value)
        {
            XmlAttribute attribute = document.CreateAttribute(attributeName);
            attribute.Value = value.ToString();
            node.Attributes.Append(attribute);
        }
        private void RaiseInventoryChangedEvent(Item item)
        {
            if (item is Weapon)
            {
                OnPropertyChanged("Weapons");
            }
            if (item is HealingPotion)
            {
                OnPropertyChanged("Potions");
            }
        }
        private void RaiseMessage(string message, bool addExtraNewLine = false) => OnMessage?.Invoke(this, new MessageEventArgs(message, addExtraNewLine));
    }
}
