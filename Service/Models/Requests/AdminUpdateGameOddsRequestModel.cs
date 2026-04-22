namespace TecmoTourney.Models.Requests
{
    public class AdminUpdateGameOddsRequestModel
    {
        public decimal Spread { get; set; }
        public int? FavoredPlayerId { get; set; }
        public decimal? MoneyLinePlayer1 { get; set; }
        public decimal? MoneyLinePlayer2 { get; set; }
        public decimal? OverUnder { get; set; }
    }
}
