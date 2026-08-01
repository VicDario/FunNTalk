using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;

namespace FunNTalk.Tests.Domain;

[TestClass]
public class UserDtoTests
{
    [TestMethod]
    public void FromEntity_MapsUsernameAndConnectionId()
    {
        var entity = new UserEntity("alice", "connection-1", "room-a");

        var dto = UserDto.FromEntity(entity);

        Assert.AreEqual("alice", dto.Username);
        Assert.AreEqual("connection-1", dto.ConnectionId);
    }

    [TestMethod]
    public void FromEntity_DoesNotLeakTheRoomOrTheEntityIdentity()
    {
        var entity = new UserEntity("bob", "connection-2", "room-b");

        var dto = UserDto.FromEntity(entity);

        Assert.AreEqual(new UserDto("bob", "connection-2"), dto);
    }
}
