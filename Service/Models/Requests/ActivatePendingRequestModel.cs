namespace TecmoTourney.Models.Requests
{
    public class ActivatePendingRequestModel
    {
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public int ProfilePic { get; set; }
    }
}
