namespace TecmoTourney.Models.Requests
{
    public class UpdateTournamentRequestModel
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<int> PlayerIds { get; set; } = new List<int>();
        public int? StatusId { get; set; }
        public string BracketData { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
