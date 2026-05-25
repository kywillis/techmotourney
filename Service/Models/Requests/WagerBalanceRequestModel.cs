using TecmoTourney;

namespace TecmoTourney.Models.Requests
{
    public class WagerBalanceRequestModel
    {
        public int PlayerId { get; set; }
        public WagerBalanceAction Action { get; set; }
        public decimal? Amount { get; set; }
    }
}
