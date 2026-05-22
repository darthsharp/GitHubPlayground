namespace GhInfo.Core.Tests.Fakes;

/// <summary>
/// A <see cref="TimeProvider"/> whose current time can be set and advanced for deterministic tests.
/// </summary>
internal sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    /// <summary>Gets or sets the current UTC time returned by <see cref="GetUtcNow"/>.</summary>
    public DateTimeOffset UtcNow { get; set; } = start;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }

    /// <summary>Advances the current time by the given amount.</summary>
    /// <param name="delta">The amount to advance.</param>
    public void Advance(TimeSpan delta)
    {
        UtcNow += delta;
    }
}
