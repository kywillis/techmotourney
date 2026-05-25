using TecmoTourney;

namespace TecmoTourney.Models.Requests
{
    public class PlaceWagerRequestModel
    {
        public int GameResultId { get; set; }
        public WagerMarketType MarketType { get; set; }
        public WagerSide Side { get; set; }
        public decimal StakeAmount { get; set; }
    }
}
