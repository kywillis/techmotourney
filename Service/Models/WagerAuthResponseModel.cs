namespace TecmoTourney.Models
{
    public class WagerAuthResponseModel
    {
        public bool IsAuthenticated { get; set; }
        public bool IsPending { get; set; }
        public string? Message { get; set; }
        public int? PlayerId { get; set; }
        public string? FullName { get; set; }
        public bool IsAdmin { get; set; }
        public decimal Balance { get; set; }
        public int? PendingActivationId { get; set; }
        public string? Email { get; set; }
        public int? RequestedProfilePic { get; set; }
        /** Set when authenticated; 0 or null = no face chosen. */
        public int? ProfilePic { get; set; }
    }
}
