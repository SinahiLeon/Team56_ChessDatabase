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
                        game.White = strings[1];
                        counter++;
                    }

                    else if (strings[0].StartsWith("[WhiteElo"))
                    {
                        game.WhiteElo = int.Parse(strings[1]);
                        counter++;
                    }

                    else if (strings[0].StartsWith("[BlackElo"))
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
                        game.EventDate = strings[1];
                        counter++;
                    }

                    else if (string.IsNullOrWhiteSpace(strings[0]))
                    {
                        counter++;
                    }
                }

                else if(counter == 10)
                {
                    if (string.IsNullOrWhiteSpace(strings[0]))
                    {
                        counter++;
                    }else
                    {
                        game.Moves += strings[0];
                    }

                }

                if (counter == 11)
                {
                    listOfGames.Add(game);
                    Console.WriteLine(game.Event + " has been added to the list!");
                    counter = 0;
                    game = new ChessGame();
                }

            }
            return listOfGames;
        }


    }
}
