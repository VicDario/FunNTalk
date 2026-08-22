namespace FunNTalk.Tests.TestDoubles;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when the test tells it to. Vacancy-TTL
/// assertions call <see cref="Advance"/> and then invoke the production code directly — no
/// <c>Thread.Sleep</c>, no timer to drive.
/// </summary>
internal sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public MutableTimeProvider(DateTimeOffset start) => _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan delta) => _utcNow += delta;
}
