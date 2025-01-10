using Newtonsoft.Json;

namespace TecmoTourney.Models
{
    public class CreatePlayerRequestModel
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; } =  string.Empty;
        [JsonProperty("fullName")]
        public string FullName { get; set; } =  string.Empty;
    }
}
