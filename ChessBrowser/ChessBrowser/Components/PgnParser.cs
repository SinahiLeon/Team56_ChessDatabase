using Microsoft.AspNetCore.Components.Forms;

namespace ChessBrowser.Components
{
    /// <summary>
    /// A class to parse PGN files and extract information into ChessGame objects.
    /// </summary>
    public class PgnParser
    {
        /// <summary>
        /// A method that iterates through all lines from a PGN file and extracts only the needed information to be stored in a ChessGame object, which will be added to and returned as a list of ChessGames.
        /// </summary>
        /// <param name="PGNFileLines">A string list of separated PGN lines to be read</param>
        /// <returns>A list of ChessGame objects extracted from the file lines</returns>
        public List<ChessGame> Parse(string[] PGNFileLines)
        {

            ChessGame game = new ChessGame();
            List<ChessGame> listOfGames = new List<ChessGame>();

            int counter = 0;

            foreach (string line in PGNFileLines)
            {
                string[] strings = line.Split('"'); // Each line is split on " to easily extract the information inside the tag without special characters
                
                // Look for 9 tags plus a blank line
                if (counter < 10) 
                {

                    if (strings[0].StartsWith("[Event "))
                    {
                        game.Event = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[Site "))
                    {
                        game.Site = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[Round "))
                    {
                        game.Round = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[White "))
                    {
                        game.White = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[Black "))
                    {
                        game.Black = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[WhiteElo "))
                    {
                        game.WhiteElo = int.Parse(strings[1]);
                        counter++;
                    }

                    else if (strings[0].StartsWith("[BlackElo "))
                    {
                        game.BlackElo = int.Parse(strings[1]);
                        counter++;
                    }

                    // Gets the Result data and converts it into a char representation of the match result
                    else if (strings[0].StartsWith("[Result "))
                    {
                        if (strings[1] == "0-1")
                        {
                            game.Result = 'B';
                        }
                        else if (strings[1] == "1-0")
                        {
                            game.Result = 'W';
                        }
                        else if (strings[1] == "1/2-1/2")
                        {
                            game.Result = 'D';
                        }
                        counter++;
                    }

                    // Gets the EventDate data and sanitizes incomplete data into a default "0000-00-00" value
                    else if (strings[0].StartsWith("[EventDate "))
                    {
                        string date = strings[1];
                        if (date.Contains("?"))
                        {
                            date = "0000-00-00";
                        }
                        game.EventDate = date;
                        counter++;
                    }

                    // Move on to reading moves when all tags have been found and a blank line is hit
                    else if (string.IsNullOrWhiteSpace(strings[0]) && counter == 9)
                    {
                        counter++; 
                    }
                }

                // Following a blank line, keep reading moves until another blank line is hit
                else if (counter == 10)
                {
                    if (string.IsNullOrWhiteSpace(strings[0]))
                    {
                        counter++;
                    }
                    else
                    {
                        game.Moves += line + "\n"; 
                    }

                }

                // Once all moves have been read, adds the ChessGame to the list and clears the ChessGame for the next game
                if (counter == 11)
                {
                    listOfGames.Add(game);
                    counter = 0;
                    game = new ChessGame();
                }

            }
            return listOfGames;
        }
    }
}
