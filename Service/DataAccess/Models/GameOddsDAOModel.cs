namespace TecmoTourney.DataAccess.Models
{
    public class GameOddsDAOModel
    {
        public int GameOddsId { get; set; }
        public int? GameResultId { get; set; }
        public int TournamentId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int BracketTypeId { get; set; }
        public int Spread { get; set; }
        public int? FavoredPlayerId { get; set; } = null;
        public string Summary { get; set; } = string.Empty;
        public int? MoneyLinePlayer1 { get; set; }
        public int? MoneyLinePlayer2 { get; set; }
        public decimal? OverUnder { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
    }
}
