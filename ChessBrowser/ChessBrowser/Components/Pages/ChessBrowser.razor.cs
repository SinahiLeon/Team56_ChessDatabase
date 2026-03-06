using Microsoft.AspNetCore.Components.Forms;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChessBrowser.Components.Pages
{
    public partial class ChessBrowser
    {
        /// <summary>
        /// Bound to the Unsername form input
        /// </summary>
        private string Username = "";

        /// <summary>
        /// Bound to the Password form input
        /// </summary>
        private string Password = "";

        /// <summary>
        /// Bound to the Database form input
        /// </summary>
        private string Database = "";

        /// <summary>
        /// Represents the progress percentage of the current
        /// upload operation. Update this value to update 
        /// the progress bar.
        /// </summary>
        private int Progress = 0;

        /// <summary>
        /// This method runs when a PGN file is selected for upload.
        /// Given a list of lines from the selected file, parses the 
        /// PGN data, and uploads each chess game to the user's database.
        /// </summary>
        /// <param name="PGNFileLines">The lines from the selected file</param>
        private async Task InsertGameData(string[] PGNFileLines)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've filled in the credentials in the GUI
            string connection = GetConnectionString();

            // TODO:
            //   Parse the provided PGN data
            //   We recommend creating separate libraries to represent chess data and load the file
            PgnParser parser = new PgnParser();
            List<ChessGame> gamesList = parser.Parse(PGNFileLines);
            Debug.WriteLine("Size of gamesList: " + gamesList.Count() + " ---------------------------");
            int totalGames = gamesList.Count();
            int gamesCount = 0;


            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO:
                    //   Iterate through your data and generate appropriate insert commands     
                    Progress = 0;
                    MySqlCommand whitePlayerCmd = conn.CreateCommand();
                    whitePlayerCmd.CommandText = "INSERT INTO Players (Name, Elo) VALUES (@name, @elo) ON DUPLICATE KEY UPDATE Elo = IF(@elo > Elo, @elo, Elo);";
                    whitePlayerCmd.Parameters.AddWithValue("@name", "");
                    whitePlayerCmd.Parameters.AddWithValue("@elo", 0);
                    whitePlayerCmd.Prepare();

                    MySqlCommand blackPlayerCmd = conn.CreateCommand();

                    blackPlayerCmd.CommandText = "INSERT INTO Players (Name, Elo) VALUES (@name, @elo) ON DUPLICATE KEY UPDATE Elo = IF(@elo > Elo, @elo, Elo);";

                    blackPlayerCmd.Parameters.AddWithValue("@name", "");
                    blackPlayerCmd.Parameters.AddWithValue("@elo", 0);

                    blackPlayerCmd.Prepare();

                    MySqlCommand eventCmd = conn.CreateCommand();
                    eventCmd.CommandText = "INSERT IGNORE INTO Events(Name, Site, Date) VALUES (@eventName, @site, @date);";

                    eventCmd.Parameters.AddWithValue("@eventName", "");
                    eventCmd.Parameters.AddWithValue("@site", "");
                    eventCmd.Parameters.AddWithValue("@date", "0000-00-00");

                    eventCmd.Prepare();

                    MySqlCommand gameCmd = conn.CreateCommand();
                    gameCmd.CommandText = "INSERT IGNORE INTO Games(Round, Result, Moves, BlackPlayer, WhitePlayer, eID) " +
                    "VALUES (@round, @result, @moves, (SELECT pID FROM Players WHERE Name=@blackName), (SELECT pID FROM Players WHERE Name=@whiteName), (SELECT eID FROM Events WHERE Name=@eventName));";

                    gameCmd.Parameters.AddWithValue("@round", "");
                    gameCmd.Parameters.AddWithValue("@result", "");
                    gameCmd.Parameters.AddWithValue("@moves", "");
                    gameCmd.Parameters.AddWithValue("@blackName", "");
                    gameCmd.Parameters.AddWithValue("@whiteName", "");
                    gameCmd.Parameters.AddWithValue("@eventName", "");

                    gameCmd.Prepare();

                    foreach (ChessGame g in gamesList)
                    {
                        // Insert white player
                        whitePlayerCmd.Parameters["@name"].Value = g.White;
                        whitePlayerCmd.Parameters["@elo"].Value = g.WhiteElo;
                        whitePlayerCmd.ExecuteNonQuery();

                        // Insert black player
                        blackPlayerCmd.Parameters["@name"].Value = g.Black;
                        blackPlayerCmd.Parameters["@elo"].Value = g.BlackElo;
                        blackPlayerCmd.ExecuteNonQuery();

                        // Insert event
                        eventCmd.Parameters["@eventName"].Value = g.Event;
                        eventCmd.Parameters["@site"].Value = g.Site;
                        eventCmd.Parameters["@date"].Value = g.EventDate;
                        eventCmd.ExecuteNonQuery();

                        // Insert game
                        gameCmd.Parameters["@round"].Value = g.Round;
                        gameCmd.Parameters["@result"].Value = g.Result;
                        gameCmd.Parameters["@moves"].Value = g.Moves;
                        gameCmd.Parameters["@blackName"].Value = g.Black;
                        gameCmd.Parameters["@whiteName"].Value = g.White;
                        gameCmd.Parameters["@eventName"].Value = g.Event;

                        gameCmd.ExecuteNonQuery();

                        //Debug.WriteLine("White: " + g.White + "whiteElo: " + g.WhiteElo + "black: " +
                        //    g.Black + "blackElo: " + g.BlackElo + "eventName: " + g.Event + "Site: " + g.Site + "date: " + g.EventDate +
                        //    "round: " + g.Round + "Result: " + g.Result);

                        gamesCount++;
                        Progress = (gamesCount * 100) / totalGames;
                    }


                    // This tells the GUI to redraw after you update Progress (this should go inside your loop)
                    await InvokeAsync(StateHasChanged);


                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

        }











        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or "" if none</param>
        /// <param name="black">The black player, or "" if none</param>
        /// <param name="opening">The first move, e.g. "1.e4", or "" if none</param>
        /// <param name="winner">The winner as "W", "B", "D", or "" if none</param>
        /// <param name="useDate">true if the filter includes a date range, false otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">true if the returned data should include the PGN moves</param>
        /// <returns>A string separated by newlines containing the filtered games</returns>
        private string PerformQuery(string white, string black, string opening,
          string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO:
                    //   Generate and execute an SQL command,
                    //   then parse the results into an appropriate string and return it.

                    string SQLCommandString;

                    // if showMoves bool = selected
                    if (showMoves)
                    {
                        SQLCommandString =
                            // Select Event: Site: Date: White: Black: Result: Moves:
                            "select Events.Name as EventName, Events.Site, Year(Events.Date) as Year, Month(Events.Date) as Month, Day(Events.Date) as Day, WhitePlayer.Name as WhiteName, WhitePlayer.Elo as WhiteElo, BlackPlayer.Name as BlackName, BlackPlayer.Elo as BlackElo, Games.Result, Games.Moves " +

                            // joing Games table to Events where eid match, and join Players where pID match
                            "from Games join Events on Events.eid=Games.eID " +
                            "join Players WhitePlayer on WhitePlayer.pID=Games.WhitePlayer " +
                            "join Players BlackPlayer on BlackPlayer.pID=Games.BlackPlayer";
                    }
                    else  // showMoves not selected
                    {
                        SQLCommandString =
                            // Select Event: Site: Date: White: Black: Result: Moves:
                            "select Events.Name as EventName, Events.Site, Year(Events.Date) as Year, Month(Events.Date) as Month, Day(Events.Date) as Day, WhitePlayer.Name as WhiteName, WhitePlayer.Elo as WhiteElo, BlackPlayer.Name as BlackName, BlackPlayer.Elo as BlackElo, Games.Result " +

                            // joing Games table to Events where eid match, and join Players where pID match
                            "from Games join Events on Events.eid=Games.eID " +
                            "join Players WhitePlayer on WhitePlayer.pID=Games.WhitePlayer " +
                            "join Players BlackPlayer on BlackPlayer.pID=Games.BlackPlayer";
                    }


                    // If white player, black player, winner, opening moves are specified in the query, they are not white space
                    bool whiteExists = !string.IsNullOrWhiteSpace(white);
                    bool blackExists = !string.IsNullOrWhiteSpace(black);
                    bool openingExists = !string.IsNullOrWhiteSpace(opening);
                    bool winnerExists = !string.IsNullOrWhiteSpace(winner);

 
                    // Determining if white or black player fields are specified --> or both are specified
                    if (whiteExists && !blackExists)
                    {
                        SQLCommandString += " where WhitePlayer.Name = '" + white + "'";
                    }
                    if (blackExists && !whiteExists)
                    {
                        SQLCommandString += " where BlackPlayer.Name = '" + black + "'";
                    }
                    if (whiteExists && blackExists)
                    {
                        SQLCommandString += " where WhitePlayer.Name = '" + white + "' and BlackPlayer.Name = '" + black + "'";
                    }


                    // If there is a winner determined from a certain color, add an and
                    if (winnerExists)
                    {
                        if (whiteExists || blackExists)
                        {
                            SQLCommandString += " and Games.Result = '" + winner + "'";
                        }
                        else    // If there is a winner determined but no color, just include the where clause
                        {
                            SQLCommandString += " where Games.Result = '" + winner + "'";
                        }
                    }


                    // If there is an opening Move that is specified and already the "where" word
                    if (openingExists)
                    {
                        if (whiteExists || blackExists || winnerExists)
                        {
                            SQLCommandString += " and Games.Moves like '" + opening + "%'";
                        }
                        else    // Opening move specified but no other field is filled in
                        {
                            SQLCommandString += " where Games.Moves like '" + opening + "%'";
                        }
                    }

                    if (useDate)
                    {
                       // DateOnly startDate = DateOnly.FromDateTime(start);
                        //  DateOnly endDate = DateOnly.FromDateTime(end);

                        if (whiteExists || blackExists || winnerExists ||openingExists)
                        {
                            //SQLCommandString += " and Date >" + start.ToString().Split(' ')[0] + " and Date < " + end.ToString().Split(' ')[0];
                            //SQLCommandString += " and Date > STR_TO_DATE(" + start.ToString().Split(' ')[0].Replace("/", "-") + ", '%m-%d-%Y') and Date < " + end.ToString().Split(' ')[0].Replace("/", "-") + ", '$m-$d-$Y')";
                            SQLCommandString += " and Date > " + start.ToString("MM-dd-yyyy") + " and Date < " + end.ToString("MM-dd-yyyy");


                        }
                        else
                        {
                            //SQLCommandString += " where Date > STR_TO_DATE(" + start.ToString().Split(' ')[0].Replace("/", "-") + ", '%m-%d-%Y') and Date < STR_TO_DATE(" + end.ToString().Split(' ')[0].Replace("/", "-") + ", '$m-$d-$Y')";
                            SQLCommandString += " where Date > " + start.ToString("MM-dd-yyyy") + " and Date < " + end.ToString("MM-dd-yyyy");

                        }

                    }

                    Debug.WriteLine("Return SQL CommandString: " + SQLCommandString);

                    MySqlCommand command = new MySqlCommand(SQLCommandString, conn);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            numRows++;
                            parsedResult +=
                                "Event: " + reader["EventName"] + "\n" +
                                "Site: " + reader["Site"] + "\n" +
                                "Date: " + reader["Month"] + "/" + reader["Day"] + "/" + reader["Year"] + "\n" +
                                "White: " + reader["WhiteName"] + " (" + reader["WhiteElo"] + ") " +"\n" +
                                "Black: " + reader["BlackName"] + " (" + reader["BlackElo"] + ") " + "\n" +
                                "Result: " + reader["Result"] + "\n";
                                
                            if (showMoves)
                            {
                                parsedResult += reader["Moves"] + "\n";
                            }
                            parsedResult += "\n";

                        }
                    }


                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            return numRows + " results\n" +"\n" + parsedResult;
        }


















        private string GetConnectionString()
        {
            return "server=atr.eng.utah.edu;database=" + Database + ";uid=" + Username + ";password=" + Password;
        }


        /// <summary>
        /// This method will run when the file chooser is used.
        /// It loads the files contents as an array of strings,
        /// then invokes the InsertGameData method.
        /// </summary>
        /// <param name="args">The event arguments, which contains the selected file name</param>
        private async void HandleFileChooser(EventArgs args)
        {
            try
            {
                string fileContent = string.Empty;

                InputFileChangeEventArgs eventArgs = args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
                if (eventArgs.FileCount == 1)
                {
                    var file = eventArgs.File;
                    if (file is null)
                    {
                        return;
                    }

                    // load the chosen file and split it into an array of strings, one per line
                    using var stream = file.OpenReadStream(1000000); // max 1MB
                    using var reader = new StreamReader(stream);
                    fileContent = await reader.ReadToEndAsync();
                    string[] fileLines = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    // insert the games, and don't wait for it to finish
                    // _ = throws away the task result, since we aren't waiting for it
                    _ = InsertGameData(fileLines);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("an error occurred while loading the file..." + e);
            }
        }

    }

}
