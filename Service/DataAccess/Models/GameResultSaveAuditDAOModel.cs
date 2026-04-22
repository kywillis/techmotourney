namespace TecmoTourney.DataAccess.Models
{
    public class GameResultSaveAuditDAOModel
    {
        public int AuditId { get; set; }
        public int GameResultId { get; set; }
        public string? SaveSource { get; set; }
        public string? ClientCorrelationId { get; set; }
        public bool IsTieGame { get; set; }
        public bool AccumulatedStats { get; set; }
        public string? RequestJson { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
