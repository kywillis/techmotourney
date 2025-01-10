namespace TecmoTourney.Models
{
    public class PlayerModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } =  string.Empty;
        public string EmailAddress { get; set; } =  string.Empty;
        public string ProfilePic { get; set; } = string.Empty;
    }
}
