using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Extensions;
using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.UseCases;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.UseCases;

[TestClass]
public class GetUsersFromRoomUseCaseTests
{
    private const string Room = "A4K9X2";

    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly GetUsersFromRoomUseCase _useCase;

    public GetUsersFromRoomUseCaseTests()
    {
        _useCase = new GetUsersFromRoomUseCase(_repository);
    }

    [TestMethod]
    public void Execute_MapsEveryParticipantOfAnExistingRoom()
    {
        _repository.GetParticipants(Room).Returns(new List<UserEntity>
        {
            new("alice", "connection-1", Room),
            new("bob", "connection-2", Room),
        });

        var users = _useCase.Execute(Room);

        CollectionAssert.AreEqual(
            new List<UserDto> { new("alice", "connection-1"), new("bob", "connection-2") },
            users);
    }

    [TestMethod]
    public void Execute_ReturnsAnEmptyListForAnEmptyRoom()
    {
        _repository.GetParticipants(Room).Returns(new List<UserEntity>());

        Assert.IsEmpty(_useCase.Execute(Room)!);
    }

    [TestMethod]
    public void Execute_ReturnsNullForAnUnknownRoom()
    {
        const string unknownRoom = "nowhere";
        // The use case normalizes before calling the repository, so the stub must be keyed to
        // the normalized form the repository will actually be asked for.
        _repository.GetParticipants(unknownRoom.NormalizeRoomCode()).Returns((IReadOnlyList<UserEntity>?)null);

        Assert.IsNull(_useCase.Execute(unknownRoom));
    }

    [TestMethod]
    public void Execute_NormalizesTheCodeBeforeLookup()
    {
        _repository.GetParticipants(Room).Returns(new List<UserEntity>
        {
            new("alice", "connection-1", Room),
        });

        var users = _useCase.Execute(" a4k9x2 ");

        Assert.IsNotNull(users);
        Assert.AreEqual("alice", Assert.ContainsSingle(users).Username);
    }
}
