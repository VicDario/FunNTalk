using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.UseCases;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.UseCases;

[TestClass]
public class GetUsersFromRoomUseCaseTests
{
    private const string Room = "room-a";

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
        _repository.GetParticipants("nowhere").Returns((IReadOnlyList<UserEntity>?)null);

        Assert.IsNull(_useCase.Execute("nowhere"));
    }
}
