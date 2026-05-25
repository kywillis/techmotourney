namespace TecmoTourney.DataAccess.Models
{
    public class WagerSettingsDAOModel
    {
        public int WagerSettingsId { get; set; }
        public bool ShowActionOnGames { get; set; }
        public decimal MaxMarketImbalance { get; set; }
    }
}
