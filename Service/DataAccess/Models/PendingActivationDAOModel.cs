using TecmoTourney;

namespace TecmoTourney.DataAccess.Models
{
    public class PendingActivationDAOModel
    {
        public int PendingActivationId { get; set; }
        public string GoogleSubjectId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RequestedProfilePic { get; set; }
        public PendingActivationStatus Status { get; set; } = PendingActivationStatus.Pending;
        public DateTime RequestedAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public int? ActivatedByPlayerId { get; set; }
    }
}
