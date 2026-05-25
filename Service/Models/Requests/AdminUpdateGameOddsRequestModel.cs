namespace TecmoTourney.Models.Requests
{
    public class AdminUpdateGameOddsRequestModel
    {
        public int Spread { get; set; }
        public int? FavoredPlayerId { get; set; }
        public int? MoneyLinePlayer1 { get; set; }
        public int? MoneyLinePlayer2 { get; set; }
        public decimal? OverUnder { get; set; }
    }
}
