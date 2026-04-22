namespace TecmoTourney.Models
{
    /// <summary>Outcome of automatic odds generation after game(s) are created.</summary>
    public class OddsGenerationStatusModel
    {
        /// <summary>True if the API tried to generate odds for at least one new game.</summary>
        public bool Attempted { get; set; }

        /// <summary>True if generation and persistence completed without failure.</summary>
        public bool Success { get; set; }

        /// <summary>User-facing detail when <see cref="Success"/> is false or partial.</summary>
        public string? Message { get; set; }
    }
}
