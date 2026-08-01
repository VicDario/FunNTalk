using FunNTalk.API.Controllers;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.UseCases;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FunNTalk.Tests.Api;

[TestClass]
public class CommunicationControllerTests
{
    private const string Room = "room-a";

    private readonly IGetUsersFromRoomUseCase _getUsersFromRoom = Substitute.For<IGetUsersFromRoomUseCase>();
    private readonly IGetIceServersUseCase _getIceServers = Substitute.For<IGetIceServersUseCase>();
    private readonly CommunicationController _controller;

    public CommunicationControllerTests()
    {
        _controller = new CommunicationController(_getUsersFromRoom, _getIceServers);
    }

    [TestMethod]
    public void GetUsersFromRoom_ReturnsOkWithTheUsersOfTheRoom()
    {
        var users = new List<UserDto> { new("alice", "connection-1"), new("bob", "connection-2") };
        _getUsersFromRoom.Execute(Room).Returns(users);

        var result = _controller.GetUsersFromRoom(Room);

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.AreSame(users, ok.Value);
    }

    [TestMethod]
    public void GetUsersFromRoom_ReturnsOkWithAnEmptyListForAnEmptyRoom()
    {
        _getUsersFromRoom.Execute(Room).Returns([]);

        var ok = Assert.IsInstanceOfType<OkObjectResult>(_controller.GetUsersFromRoom(Room));

        Assert.IsEmpty(Assert.IsInstanceOfType<List<UserDto>>(ok.Value));
    }

    [TestMethod]
    public void GetUsersFromRoom_ReturnsNotFoundWhenTheRoomDoesNotExist()
    {
        _getUsersFromRoom.Execute("nowhere").Returns((List<UserDto>?)null);

        var result = _controller.GetUsersFromRoom("nowhere");

        var notFound = Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        Assert.IsNotNull(notFound.Value);
    }

    [TestMethod]
    public async Task GetIceServers_ReturnsOkWithWhateverTheUseCaseProduced()
    {
        IReadOnlyList<IceServerDto> iceServers =
        [
            new("stun:stun.l.google.com:19302"),
            new("turn:relay.example.com:80") { Username = "minted-user", Credential = "minted-secret" },
        ];
        _getIceServers.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(iceServers);

        var result = await _controller.GetIceServers(CancellationToken.None);

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.AreSame(iceServers, ok.Value);
    }

    [TestMethod]
    public async Task GetIceServers_PassesTheRequestCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();
        _getIceServers.ExecuteAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _controller.GetIceServers(cancellation.Token);

        await _getIceServers.Received(1).ExecuteAsync(cancellation.Token);
    }
}
