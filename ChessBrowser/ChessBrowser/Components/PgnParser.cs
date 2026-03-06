using Microsoft.AspNetCore.Components.Forms;

namespace ChessBrowser.Components
{
    public class PgnParser
    {
        public List<ChessGame> Parse(string[] PGNFileLines)
        {

            ChessGame game = new ChessGame();
            List<ChessGame> listOfGames = new List<ChessGame>();

            int counter = 0;

            foreach (string line in PGNFileLines)
            {
                string[] strings = line.Split('"');
                if (counter < 10)   // Want to track 9 tags + Moves
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

                    else if (string.IsNullOrWhiteSpace(strings[0]) && counter == 9)
                    {
                        counter++;  // All 9 tags were found, now store Moves
                    }
                }

                else if(counter == 10)
                {
                    if (string.IsNullOrWhiteSpace(strings[0]))
                    {
                        counter++;  // Counter == 11, end of game
                    }else
                    {
                        game.Moves += line + "\n";  // we are iterating for each line, add full line to game.Moves
                    }

                }

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
