namespace ChessBrowser.Components
{
    public class ChessGame
    {
        public string Event
        {
            get; set;
        }

        public string Site
        {
            get; set;
        }

        public string Round
        {
            // "not always convertible to a number since sometimes there are two decimal points, such as 1.4.10"
            get; set;
        }

        public string White
        {
            get; set;
        }

        public string Black
        {
            get; set;
        }

        public char Result
        {
            get; set;
        }

        public int WhiteElo
        {
            get; set;
        }

        public int BlackElo
        {
            get; set;
        }

        public string EventDate
        {
            get; set;
        }

        public string Moves
        {
            get; set;
        }
    }
}
