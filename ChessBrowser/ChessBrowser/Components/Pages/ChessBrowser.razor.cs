using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using MySql.Data.MySqlClient;

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
                    MySqlCommand command = conn.CreateCommand();
                    command.CommandText =
                    "insert into Players (Name, Elo) " +
                    "values (@whiteName, @whiteElo) on duplicate key update Elo = if(@whiteElo > Elo, @whiteElo, Elo); " +
                    "insert into Players (Name, Elo) " +
                    "values (@blackName, @eblackElo) on duplicate key update Elo = if(@blackElo > Elo, @blackElo, Elo);" +
                    "insert ignore into Events(Name, Site, Date) " +
                    "values (@eventName, @site, @date) ;" +
                    "insert ignore into Games(Round, Result, Moves, BlackPlayer, WhitePlayer, eID) " +
                    "values (@round, @result, @moves, (select pID from Players where Name=@blackName), (select pID from Players where Name=@whiteName), (select eID from Events where Name=@eventName));";


                    command.Parameters.AddWithValue("@whiteName", "whiteName");
                    command.Parameters.AddWithValue("@whiteElo", 1000);
                    command.Parameters.AddWithValue("@blackName", "blackName");
                    command.Parameters.AddWithValue("@blackElo", 1000);

                    command.Parameters.AddWithValue("@eventName", "eventName");
                    command.Parameters.AddWithValue("@site", "site");
                    command.Parameters.AddWithValue("@date", "0000-00-00");

                    command.Parameters.AddWithValue("@round", "roundString");
                    command.Parameters.AddWithValue("@result", "c");
                    command.Parameters.AddWithValue("@moves", "movesString");


                    command.Prepare();



                    foreach (ChessGame g in gamesList)
                    {
                        command.Parameters["@whiteName"].Value = g.White;
                        command.Parameters["@whiteElo"].Value = g.WhiteElo;
                        command.Parameters["@blackName"].Value = g.Black;
                        command.Parameters["@blackElo"].Value = g.BlackElo;

                        command.Parameters["@eventName"].Value = g.Event;
                        command.Parameters["@site"].Value = g.Site;
                        command.Parameters["@date"].Value = g.EventDate;

                        command.Parameters["@round"].Value = g.Round;
                        command.Parameters["@result"].Value = g.Result;
                        command.Parameters["@moves"].Value = g.Moves;

                        Debug.WriteLine("White: " + g.White + "whiteElo: " + g.WhiteElo + "black: " + 
                            g.Black + "blackElo: " + g.BlackElo + "eventName: " + g.Event + "Site: " + g.Site + "date: " + g.EventDate +
                            "round: " + g.Round + "Result: " + g.Result);

                        int result = command.ExecuteNonQuery();
                        
                        Debug.WriteLine(result + "*************************");

                        gamesCount++;
                        Progress = (gamesCount / totalGames) * 100;
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
