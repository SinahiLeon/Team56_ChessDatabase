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

                    string SQLCommandString = "select * from Games join Events on Events.eID=Games.eID";                        //"join Players as p1 on Games.BlackPlayer=(select pID from p1 where Name=" + black + ")" +
                        //"join Players as p2 on Games.WhitePlayer=(select pID from p2 where Name=" + white + ")" +
                        //"where Result=" + winner + "and Moves like '" + opening + "%'" + "and Date < " + start +
                        //"and Date < " + end;

               
                    if (!string.IsNullOrWhiteSpace(white))
                    {
                        SQLCommandString += " join Players as p1 on Games.WhitePlayer=(select pID from p1 where Name=" + white + ")";
                    }
                    if (!string.IsNullOrWhiteSpace(black))
                    {
                        SQLCommandString += " join Players as p2 on Games.BlackPlayer=(select pID from p2 where Name=" + black + ")";

                    }
                    bool winnerExists = !string.IsNullOrWhiteSpace(winner);
                    bool openingExists = !string.IsNullOrWhiteSpace(opening);
                    if(winnerExists || openingExists || useDate)
                    {
                        SQLCommandString += " where";
                        if (winnerExists)
                        {
                            SQLCommandString += " Result=" + winner;
                        }
                        if (openingExists)
                        {
                            if (winnerExists)
                            {
                                SQLCommandString += " and";
                            }
                            SQLCommandString += " Moves like '" + opening + "%'";
                        }
                        if (useDate)
                        {
                            if(winnerExists || openingExists)
                            {
                                SQLCommandString += " and";
                            }
                            SQLCommandString += " Date > " + start + "and Date < " + end;
                        }
                    }

                    MySqlCommand command = new MySqlCommand(SQLCommandString, conn);

                    using(MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                        }
                    }


                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            return numRows + " results\n" + parsedResult;
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
