namespace TecmoTourney.DataAccess.Models
{
    public class PlayerDAOModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } =  string.Empty;
        public int ProfilePic { get; set; }
        public string Profile { get; set; } = string.Empty;
    }
}
