namespace TecmoTourney.Models
{
    public class PlayerModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } =  string.Empty;
        public string EmailAddress { get; set; } =  string.Empty;
        public int ProfilePic { get; set; }
        public string Profile { get; set; } = string.Empty;
    }
}
