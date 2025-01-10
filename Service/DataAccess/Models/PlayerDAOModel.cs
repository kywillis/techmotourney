namespace TecmoTourney.DataAccess.Models
{
    public class PlayerDAOModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } =  string.Empty;
        public string ProfilePic { get; set; } = string.Empty;
    }
}
