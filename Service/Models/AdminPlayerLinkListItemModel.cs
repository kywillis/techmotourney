namespace TecmoTourney.Models
{
    /// <summary>Player row eligible to receive a Google link (no GoogleSubjectId, not deleted).</summary>
    public class AdminPlayerLinkListItemModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}
