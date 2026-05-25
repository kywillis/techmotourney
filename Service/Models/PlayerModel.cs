namespace TecmoTourney.Models
{
    public class PlayerModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } =  string.Empty;
        public string EmailAddress { get; set; } =  string.Empty;
        public int ProfilePic { get; set; }
        public string Profile { get; set; } = string.Empty;
        public string? GoogleSubjectId { get; set; }
        public bool IsAdmin { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }
}
