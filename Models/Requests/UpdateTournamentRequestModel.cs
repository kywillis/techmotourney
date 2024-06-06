namespace TecmoTourney.Models.Requests
{
    public class UpdateTournamentRequestModel
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
