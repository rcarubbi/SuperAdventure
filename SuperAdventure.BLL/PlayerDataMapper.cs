using System;
using System.Data;
using System.Data.SqlClient;

namespace SuperAdventure.BLL
{
    public static class PlayerDataMapper
    {
        private static readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SuperAdventure;Integrated Security=True";
        public static Player CreateFromDatabase()
        {
            try
            {
                // This is our connection to the database
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Open the connection, so we can perform SQL commands
                    connection.Open();
                    Player player;
                    // Create a SQL command object, that uses the connection to our database
                    // The SqlCommand object is where we create our SQL statement
                    using (SqlCommand savedGameCommand = connection.CreateCommand())
                    {
                        savedGameCommand.CommandType = CommandType.Text;
                        // This SQL statement reads the first rows in teh SavedGame table.
                        // For this program, we should only ever have one row,
                        // but this will ensure we only get one record in our SQL query results.
                        savedGameCommand.CommandText = "SELECT TOP 1 * FROM SavedGame";
                        // Use ExecuteReader when you expect the query to return a row, or rows
                        SqlDataReader reader = savedGameCommand.ExecuteReader();
                        // Check if the query did not return a row/record of data
                        if (!reader.HasRows)
                        {
                            // There is no data in the SavedGame table, 
                            // so return null (no saved player data)
                            return null;
                        }
                        // Get the row/record from the data reader
                        reader.Read();
                        // Get the column values for the row/record
                        int currentHitPoints = (int)reader["CurrentHitPoints"];
                        int maximumHitPoints = (int)reader["MaximumHitPoints"];
                        int gold = (int)reader["Gold"];
                        int experiencePoints = (int)reader["ExperiencePoints"];
                        int currentLocationId = (int)reader["CurrentLocationId"];
                        // Create the Player object, with the saved game values
                        player = Player.CreatePlayerFromDatabase(currentHitPoints, maximumHitPoints, gold,
                            experiencePoints, currentLocationId);
                    }
                    // Read the rows/records from the Quest table, and add them to the player
                    using (SqlCommand questCommand = connection.CreateCommand())
                    {
                        questCommand.CommandType = CommandType.Text;
                        questCommand.CommandText = "SELECT * FROM Quest";
                        SqlDataReader reader = questCommand.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int questId = (int)reader["QuestId"];
                                bool isCompleted = (bool)reader["IsCompleted"];
                                // Build the PlayerQuest item, for this row
                                PlayerQuest playerQuest = new PlayerQuest(World.QuestById(questId));
                                playerQuest.IsCompleted = isCompleted;
                                // Add the PlayerQuest to the player's property
                                player.Quests.Add(playerQuest);
                            }
                        }
                    }
                    // Read the rows/records from the Inventory table, and add them to the player
                    using (SqlCommand inventoryCommand = connection.CreateCommand())
                    {
                        inventoryCommand.CommandType = CommandType.Text;
                        inventoryCommand.CommandText = "SELECT * FROM Inventory";
                        SqlDataReader reader = inventoryCommand.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int inventoryItemID = (int)reader["InventoryItemId"];
                                int quantity = (int)reader["Quantity"];
                                // Add the item to the player's inventory
                                player.AddItemToInventory(World.ItemById(inventoryItemID), quantity);
                            }
                        }
                    }
                    // Now that the player has been built from the database, return it.
                    return player;
                }
            }
            catch
            {
                // Ignore errors. If there is an error, this function will return a "null" player.
            }
            return null;
        }
        public static void SaveToDatabase(Player player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Open the connection, so we can perform SQL commands
                    connection.Open();
                    // Insert/Update data in SavedGame table
                    using (SqlCommand existingRowCountCommand = connection.CreateCommand())
                    {
                        existingRowCountCommand.CommandType = CommandType.Text;
                        existingRowCountCommand.CommandText = "SELECT count(*) FROM SavedGame";
                        // Use ExecuteScalar when your query will return one value
                        int existingRowCount = (int)existingRowCountCommand.ExecuteScalar();
                        if (existingRowCount == 0)
                        {
                            // There is no existing row, so do an INSERT
                            using (SqlCommand insertSavedGame = connection.CreateCommand())
                            {
                                insertSavedGame.CommandType = CommandType.Text;
                                insertSavedGame.CommandText =
                                    "INSERT INTO SavedGame " +
                                    "(CurrentHitPoints, MaximumHitPoints, Gold, ExperiencePoints, CurrentLocationId) " +
                                    "VALUES " +
                                    "(@CurrentHitPoints, @MaximumHitPoints, @Gold, @ExperiencePoints, @CurrentLocationId)";
                                // Pass the values from the player object, to the SQL query, using parameters
                                insertSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                insertSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                insertSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                insertSavedGame.Parameters["@Gold"].Value = player.Gold;
                                insertSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                insertSavedGame.Parameters.Add("@CurrentLocationId", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentLocationId"].Value = player.CurrentLocation.Id;
                                // Perform the SQL command.
                                // Use ExecuteNonQuery, because this query does not return any results.
                                insertSavedGame.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // There is an existing row, so do an UPDATE
                            using (SqlCommand updateSavedGame = connection.CreateCommand())
                            {
                                updateSavedGame.CommandType = CommandType.Text;
                                updateSavedGame.CommandText =
                                    "UPDATE SavedGame " +
                                    "SET CurrentHitPoints = @CurrentHitPoints, " +
                                    "MaximumHitPoints = @MaximumHitPoints, " +
                                    "Gold = @Gold, " +
                                    "ExperiencePoints = @ExperiencePoints, " +
                                    "CurrentLocationId = @CurrentLocationId";
                                // Pass the values from the player object, to the SQL query, using parameters
                                // Using parameters helps make your program more secure.
                                // It will prevent SQL injection attacks.
                                updateSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                updateSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                updateSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                updateSavedGame.Parameters["@Gold"].Value = player.Gold;
                                updateSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                updateSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                updateSavedGame.Parameters.Add("@CurrentLocationId", SqlDbType.Int);
                                updateSavedGame.Parameters["@CurrentLocationId"].Value = player.CurrentLocation.Id;
                                // Perform the SQL command.
                                // Use ExecuteNonQuery, because this query does not return any results.
                                updateSavedGame.ExecuteNonQuery();
                            }
                        }
                    }
                    // The Quest and Inventory tables might have more, or less, rows in the database
                    // than what the player has in their properties.
                    // So, when we save the player's game, we will delete all the old rows
                    // and add in all new rows.
                    // This is easier than trying to add/delete/update each individual rows
                    // Delete existing Quest rows
                    using (SqlCommand deleteQuestsCommand = connection.CreateCommand())
                    {
                        deleteQuestsCommand.CommandType = CommandType.Text;
                        deleteQuestsCommand.CommandText = "DELETE FROM Quest";
                        deleteQuestsCommand.ExecuteNonQuery();
                    }
                    // Insert Quest rows, from the player object
                    foreach (PlayerQuest playerQuest in player.Quests)
                    {
                        using (SqlCommand insertQuestCommand = connection.CreateCommand())
                        {
                            insertQuestCommand.CommandType = CommandType.Text;
                            insertQuestCommand.CommandText = "INSERT INTO Quest (QuestId, IsCompleted) VALUES (@QuestId, @IsCompleted)";
                            insertQuestCommand.Parameters.Add("@QuestId", SqlDbType.Int);
                            insertQuestCommand.Parameters["@QuestId"].Value = playerQuest.Details.Id;
                            insertQuestCommand.Parameters.Add("@IsCompleted", SqlDbType.Bit);
                            insertQuestCommand.Parameters["@IsCompleted"].Value = playerQuest.IsCompleted;
                            insertQuestCommand.ExecuteNonQuery();
                        }
                    }
                    // Delete existing Inventory rows
                    using (SqlCommand deleteInventoryCommand = connection.CreateCommand())
                    {
                        deleteInventoryCommand.CommandType = CommandType.Text;
                        deleteInventoryCommand.CommandText = "DELETE FROM Inventory";
                        deleteInventoryCommand.ExecuteNonQuery();
                    }
                    // Insert Inventory rows, from the player object
                    foreach (InventoryItem inventoryItem in player.Inventory)
                    {
                        using (SqlCommand insertInventoryCommand = connection.CreateCommand())
                        {
                            insertInventoryCommand.CommandType = CommandType.Text;
                            insertInventoryCommand.CommandText = "INSERT INTO Inventory (InventoryItemId, Quantity) VALUES (@InventoryItemId, @Quantity)";
                            insertInventoryCommand.Parameters.Add("@InventoryItemId", SqlDbType.Int);
                            insertInventoryCommand.Parameters["@InventoryItemId"].Value = inventoryItem.Details.Id;
                            insertInventoryCommand.Parameters.Add("@Quantity", SqlDbType.Int);
                            insertInventoryCommand.Parameters["@Quantity"].Value = inventoryItem.Quantity;
                            insertInventoryCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch 
            {
                // We are going to ignore errors, for now.
            }
        }
    }
}
