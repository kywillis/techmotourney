namespace TecmoTourney.Models.Requests
{
    public class GameStationUpdateRequestModel
    {
        /// <summary>When the game is Waiting, must be true to transition to InProgress.</summary>
        public bool StartGame { get; set; }

        /// <summary>When the game is InProgress, set true to move it back to Waiting (clears started time).</summary>
        public bool RevertToWaiting { get; set; }

        public int Player1GameTeamId { get; set; }

        public int Player2GameTeamId { get; set; }
    }
}
