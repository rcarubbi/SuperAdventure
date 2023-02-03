using SuperAdventure.BLL;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using static System.Console;
namespace SuperAdventure.Console
{
    internal class Program
    {
        private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";
        private static Player _player;
        private static void Main(string[] args)
        {

            LoadGameData();

            WriteLine("Type 'Help' to see a list of commands");
            WriteLine("");

            DisplayCurrentLocation();

            // Connect player events to functions that will display in the UI
            _player.PropertyChanged += Player_OnPropertyChanged;
            _player.OnMessage += Player_OnMessage;

            // Infinite loop, until the user types "exit"
            while (true)
            {
                // Display a prompt, so the user knows to type something
                Write(">");
                // Wait for the user to type something, and press the <Enter> key
                string userInput = ReadLine();
                // If they typed a blank line, loop back and wait for input again
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                // Convert to lower-case, to make comparisons easier
                string cleanedInput = userInput.ToLower();

                // Save the current game data, and break out of the "while(true)" loop
                if (cleanedInput == "exit")
                {
                    SaveGameData();
                    break;
                }

                // If the user typed something, try to determine what to do
                ParseInput(cleanedInput);
            }
        }
        private static void Player_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentLocation")
            {
                DisplayCurrentLocation();
                if (_player.CurrentLocation.VendorWorkingHere != null)
                {
                    WriteLine("You see a vendor here: {0}", _player.CurrentLocation.VendorWorkingHere.Name);
                }
            }
        }
        private static void Player_OnMessage(object sender, MessageEventArgs e)
        {
            WriteLine(e.Message);
            if (e.AddExtraNewLine)
            {
                WriteLine("");
            }
        }
        private static void ParseInput(string input)
        {
            if (input.Contains("help") || input == "?")
            {
                DisplayHelpText();
            }
            else if (input == "stats")
            {
                DisplayPlayerStats();
            }
            else if (input == "look")
            {
                DisplayCurrentLocation();
            }
            else if (input.Contains("north"))
            {
                if (_player.CurrentLocation.LocationToNorth == null)
                {
                    WriteLine("You cannot move North");
                }
                else
                {
                    _player.MoveNorth();
                }
            }
            else if (input.Contains("east"))
            {
                if (_player.CurrentLocation.LocationToEast == null)
                {
                    WriteLine("You cannot move East");
                }
                else
                {
                    _player.MoveEast();
                }
            }
            else if (input.Contains("south"))
            {
                if (_player.CurrentLocation.LocationToSouth == null)
                {
                    WriteLine("You cannot move South");
                }
                else
                {
                    _player.MoveSouth();
                }
            }
            else if (input.Contains("west"))
            {
                if (_player.CurrentLocation.LocationToWest == null)
                {
                    WriteLine("You cannot move West");
                }
                else
                {
                    _player.MoveWest();
                }
            }
            else if (input == "inventory")
            {
                foreach (InventoryItem inventoryItem in _player.Inventory)
                {
                    WriteLine("{0}: {1}", inventoryItem.Description, inventoryItem.Quantity);
                }
            }
            else if (input == "quests")
            {
                if (_player.Quests.Count == 0)
                {
                    WriteLine("You do not have any quests");
                }
                else
                {
                    foreach (PlayerQuest playerQuest in _player.Quests)
                    {
                        WriteLine("{0}: {1}", playerQuest.Name,
                            playerQuest.IsCompleted ? "Completed" : "Incomplete");
                    }
                }
            }
            else if (input.Contains("attack"))
            {
                AttackMonster();
            }
            else if (input.StartsWith("equip "))
            {
                EquipWeapon(input);
            }
            else if (input.StartsWith("drink "))
            {
                DrinkPotion(input);
            }
            else if (input == "trade")
            {
                ViewTradeInventory();
            }
            else if (input.StartsWith("buy "))
            {
                BuyItem(input);
            }
            else if (input.StartsWith("sell "))
            {
                SellItem(input);
            }
            else
            {
                WriteLine("I do not understand");
                WriteLine("Type 'Help' to see a list of available commands");
            }
            // Write a blank line, to keep the UI a little cleaner
            WriteLine("");
        }
        private static void DisplayHelpText()
        {
            WriteLine("Available commands");
            WriteLine("====================================");
            WriteLine("Stats - Display player information");
            WriteLine("Look - Get the description of your location");
            WriteLine("Inventory - Display your inventory");
            WriteLine("Quests - Display your quests");
            WriteLine("Attack - Fight the monster");
            WriteLine("Equip <weapon name> - Set your current weapon");
            WriteLine("Drink <potion name> - Drink a potion");
            WriteLine("Trade - display your inventory and vendor's inventory");
            WriteLine("Buy <item name> - Buy an item from a vendor");
            WriteLine("Sell <item name> - Sell an item to a vendor");
            WriteLine("North - Move North");
            WriteLine("South - Move South");
            WriteLine("East - Move East");
            WriteLine("West - Move West");
            WriteLine("Exit - Save the game and exit");
        }
        private static void DisplayPlayerStats()
        {
            WriteLine("Current hit points: {0}", _player.CurrentHitPoints);
            WriteLine("Maximum hit points: {0}", _player.MaximumHitPoints);
            WriteLine("Experience Points: {0}", _player.ExperiencePoints);
            WriteLine("Level: {0}", _player.Level);
            WriteLine("Gold: {0}", _player.Gold);
        }
        private static void AttackMonster()
        {
            if (_player.CurrentLocation.MonsterLivingHere == null)
            {
                WriteLine("There is nothing here to attack");
            }
            else
            {
                if (_player.CurrentWeapon == null)
                {
                    // Select the first weapon in the player's inventory 
                    // (or 'null', if they do not have any weapons)
                    _player.CurrentWeapon = _player.Weapons.FirstOrDefault();
                }
                if (_player.CurrentWeapon == null)
                {
                    WriteLine("You do not have any weapons");
                }
                else
                {
                    _player.UseWeapon(_player.CurrentWeapon);
                }
            }
        }
        private static void EquipWeapon(string input)
        {
            string inputWeaponName = input.Substring(6).Trim();
            if (string.IsNullOrEmpty(inputWeaponName))
            {
                WriteLine("You must enter the name of the weapon to equip");
            }
            else
            {
                Weapon weaponToEquip =
                    _player.Weapons.SingleOrDefault(
                        x => x.Name.ToLower() == inputWeaponName || x.NamePlural.ToLower() == inputWeaponName);
                if (weaponToEquip == null)
                {
                    WriteLine("You do not have the weapon: {0}", inputWeaponName);
                }
                else
                {
                    _player.CurrentWeapon = weaponToEquip;
                    WriteLine("You equip your {0}", _player.CurrentWeapon.Name);
                }
            }
        }
        private static void DrinkPotion(string input)
        {
            string inputPotionName = input.Substring(6).Trim();
            if (string.IsNullOrEmpty(inputPotionName))
            {
                WriteLine("You must enter the name of the potion to drink");
            }
            else
            {
                HealingPotion potionToDrink =
                    _player.Potions.SingleOrDefault(
                        x => x.Name.ToLower() == inputPotionName || x.NamePlural.ToLower() == inputPotionName);
                if (potionToDrink == null)
                {
                    WriteLine("You do not have the potion: {0}", inputPotionName);
                }
                else
                {
                    _player.UsePotion(potionToDrink);
                }
            }
        }
        private static void ViewTradeInventory()
        {
            if (LocationDoesNotHaveVendor())
            {
                WriteLine("There is no vendor here");
            }
            else
            {
                WriteLine("PLAYER INVENTORY");
                WriteLine("================");
                if (_player.Inventory.Count(x => x.Price != World.UNSELLABLE_ITEM_PRICE) == 0)
                {
                    WriteLine("You do not have any inventory");
                }
                else
                {
                    foreach (
                        InventoryItem inventoryItem in _player.Inventory.Where(x => x.Price != World.UNSELLABLE_ITEM_PRICE))
                    {
                        WriteLine("{0} {1} Price: {2}", inventoryItem.Quantity, inventoryItem.Description,
                            inventoryItem.Price);
                    }
                }
                WriteLine("");
                WriteLine("VENDOR INVENTORY");
                WriteLine("================");
                if (_player.CurrentLocation.VendorWorkingHere.Inventory.Count == 0)
                {
                    WriteLine("The vendor does not have any inventory");
                }
                else
                {
                    foreach (InventoryItem inventoryItem in _player.CurrentLocation.VendorWorkingHere.Inventory)
                    {
                        WriteLine("{0} {1} Price: {2}", inventoryItem.Quantity, inventoryItem.Description,
                            inventoryItem.Price);
                    }
                }
            }
        }
        private static void BuyItem(string input)
        {
            if (LocationDoesNotHaveVendor())
            {
                return;
            }
            else
            {
                string itemName = input.Substring(4).Trim();
                if (string.IsNullOrEmpty(itemName))
                {
                    WriteLine("You must enter the name of the item to buy");
                }
                else
                {
                    // Get the InventoryItem from the trader's inventory
                    InventoryItem itemToBuy =
                        _player.CurrentLocation.VendorWorkingHere.Inventory.SingleOrDefault(
                            x => x.Details.Name.ToLower() == itemName);
                    // Check if the vendor has the item
                    if (itemToBuy == null)
                    {
                        WriteLine("The vendor does not have any {0}", itemName);
                    }
                    else
                    {
                        // Check if the player has enough gold to buy the item
                        if (_player.Gold < itemToBuy.Price)
                        {
                            WriteLine("You do not have enough gold to buy a {0}", itemToBuy.Description);
                        }
                        else
                        {
                            // Success! Buy the item
                            _player.AddItemToInventory(itemToBuy.Details);
                            _player.Gold -= itemToBuy.Price;
                            WriteLine("You bought one {0} for {1} gold", itemToBuy.Details.Name, itemToBuy.Price);
                        }
                    }
                }
            }
        }
        private static void SellItem(string input)
        {
            if (LocationDoesNotHaveVendor())
            {
                return;
            }
            else
            {
                string itemName = input.Substring(5).Trim();
                if (string.IsNullOrEmpty(itemName))
                {
                    WriteLine("You must enter the name of the item to sell");
                }
                else
                {
                    // Get the InventoryItem from the player's inventory
                    InventoryItem itemToSell =
                        _player.Inventory.SingleOrDefault(x => x.Details.Name.ToLower() == itemName &&
                                                               x.Quantity > 0 &&
                                                               x.Price != World.UNSELLABLE_ITEM_PRICE);
                    // Check if the player has the item entered
                    if (itemToSell == null)
                    {
                        WriteLine("The player cannot sell any {0}", itemName);
                    }
                    else
                    {
                        // Sell the item
                        _player.RemoveItemFromInventory(itemToSell.Details);
                        _player.Gold += itemToSell.Price;
                        WriteLine("You receive {0} gold for your {1}", itemToSell.Price, itemToSell.Details.Name);
                    }
                }
            }
        }
        private static bool LocationDoesNotHaveVendor()
        {
            bool locationDoesNotHaveVendor = _player.CurrentLocation.VendorWorkingHere == null;

            WriteLine("There is no vendor at this location");

            return locationDoesNotHaveVendor;
        }
        private static void DisplayCurrentLocation()
        {
            WriteLine("You are at: {0}", _player.CurrentLocation.Name);
            if (_player.CurrentLocation.Description != "")
            {
                WriteLine(_player.CurrentLocation.Description);
            }
        }
        private static void LoadGameData()
        {
            _player = PlayerDataMapper.CreateFromDatabase();
            if (_player == null)
            {
                if (File.Exists(PLAYER_DATA_FILE_NAME))
                {
                    _player = Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME));
                }
                else
                {
                    _player = Player.CreateDefaultPlayer();
                }
            }
        }
        private static void SaveGameData()
        {
            File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
            PlayerDataMapper.SaveToDatabase(_player);
        }
    }
}
