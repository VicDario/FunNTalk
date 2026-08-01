using System.Text.Json;
using FunNTalk.Domain.DTOs;

namespace FunNTalk.Tests.Domain;

/// <summary>
/// The browser's RTCIceServer only accepts "urls", "username" and "credential", and it rejects
/// a STUN entry that carries null credentials, so the wire shape is the contract under test.
/// </summary>
[TestClass]
public class IceServerDtoTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [TestMethod]
    public void Serializes_TurnEntry_AsUrlsUsernameAndCredential()
    {
        var dto = new IceServerDto("turn:relay.example.com:80")
        {
            Username = "minted-user",
            Credential = "minted-secret",
        };

        var json = JsonSerializer.Serialize(dto, SerializerOptions);

        Assert.AreEqual(
            """{"urls":"turn:relay.example.com:80","username":"minted-user","credential":"minted-secret"}""",
            json);
    }

    [TestMethod]
    public void Serializes_StunEntry_WithoutUsernameOrCredential()
    {
        var dto = new IceServerDto("stun:stun.l.google.com:19302");

        var json = JsonSerializer.Serialize(dto, SerializerOptions);

        Assert.AreEqual("""{"urls":"stun:stun.l.google.com:19302"}""", json);
        Assert.DoesNotContain("username", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Serializes_EntryWithOnlyUsername_WithoutCredential()
    {
        var dto = new IceServerDto("turn:relay.example.com:443") { Username = "minted-user" };

        var json = JsonSerializer.Serialize(dto, SerializerOptions);

        Assert.AreEqual("""{"urls":"turn:relay.example.com:443","username":"minted-user"}""", json);
    }
}
