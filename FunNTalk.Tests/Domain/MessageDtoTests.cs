using System.Text.Json;
using FunNTalk.Domain.DTOs;

namespace FunNTalk.Tests.Domain;

[TestClass]
public class MessageDtoTests
{
    [TestMethod]
    public void Timestamp_SerializesAsAnIso8601StringTheClientCanParse()
    {
        var timestamp = new DateTimeOffset(2024, 5, 17, 8, 9, 10, TimeSpan.Zero);
        var message = new MessageDto(Guid.NewGuid(), timestamp, new UserDto("alice", "connection-1"), "hello");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(message));
        var serializedTimestamp = document.RootElement.GetProperty("Timestamp");

        Assert.AreEqual(JsonValueKind.String, serializedTimestamp.ValueKind);
        var raw = serializedTimestamp.GetString()!;
        Assert.AreEqual(timestamp, DateTimeOffset.Parse(raw, System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Id_SerializesAsAStableIdentityDistinctFromTheTimestamp()
    {
        var id = Guid.NewGuid();
        var message = new MessageDto(id, DateTimeOffset.UtcNow, new UserDto("alice", "connection-1"), "hello");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(message));

        Assert.AreEqual(id, document.RootElement.GetProperty("Id").GetGuid());
    }

    [TestMethod]
    public void TwoMessagesInTheSameInstant_StillHaveDistinctIdentities()
    {
        var instant = DateTimeOffset.UtcNow;
        var user = new UserDto("alice", "connection-1");

        var first = new MessageDto(Guid.NewGuid(), instant, user, "hello");
        var second = new MessageDto(Guid.NewGuid(), instant, user, "hello");

        Assert.AreNotEqual(first.Id, second.Id);
        Assert.AreNotEqual(first, second);
    }
}
