namespace ChessBrowser.Components
{
    /// <summary>
    /// A class to represent a chess game. Supports in extracting information out of a PGN into C# data types.
    /// </summary>
    public class ChessGame
    {
        /// <summary>
        /// Event name property to be represented in the Events table
        /// </summary>
        public string Event
        {
            get; set;
        }

        /// <summary>
        /// Event site property to be represented in the Events table
        /// </summary>
        public string Site
        {
            get; set;
        }

        /// <summary>
        /// Game round property to be represented in the Games table.
        /// </summary>
        public string Round
        {
            // "not always convertible to a number since sometimes there are two decimal points, such as 1.4.10"
            get; set;
        }

        /// <summary>
        /// White Player name property to be represented in the Players table.
        /// </summary>
        public string White
        {
            get; set;
        }

        /// <summary>
        /// Black Player name property to be represented in the Players table.
        /// </summary>
        public string Black
        {
            get; set;
        }

        /// <summary>
        /// Game result property to be represented in the Games table.
        /// </summary>
        public char Result
        {
            get; set;
        }

        /// <summary>
        /// White Player elo property to be represented in the Players table.
        /// </summary>
        public int WhiteElo
        {
            get; set;
        }


        /// <summary>
        /// Black Player elo property to be represented in the Players table.
        /// </summary>
        public int BlackElo
        {
            get; set;
        }

        /// <summary>
        /// Event date property to be represented in the Events table.
        /// </summary>
        public string EventDate
        {
            get; set;
        }

        /// <summary>
        /// Game moves property to be represented in the Games table.
        /// </summary>
        public string Moves
        {
            get; set;
        }
    }
}
