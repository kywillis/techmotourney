namespace TecmoTourney.Models
{
    /// <summary>Minimal player row for admin balance UI (dropdown + current balance).</summary>
    public class AdminPlayerBalanceListItemModel
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
