namespace TecmoTourney;

/// <summary>Helpers for times stored in the DB and exposed as JSON. Game start uses <see cref="DateTime.UtcNow"/>.</summary>
public static class GameClockTime
{
    /// <summary>
    /// <c>TC_GameResults.GameStartedAt</c> is always written with <see cref="DateTime.UtcNow"/>;
    /// Dapper returns <see cref="DateTimeKind.Unspecified"/>, which makes System.Text.Json omit "Z" and breaks browser parsing.
    /// </summary>
    public static DateTime? AsUtcForJson(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
