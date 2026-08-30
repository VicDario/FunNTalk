using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Configuration;
using FunNTalk.Infrastructure.Services;
using FunNTalk.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.Services;

[TestClass]
public class RoomReaperServiceTests
{
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly MutableTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly RoomReaperService _service;

    public RoomReaperServiceTests()
    {
        _service = new RoomReaperService(
            _repository,
            _time,
            Options.Create(new RoomOptions()),
            NullLogger<RoomReaperService>.Instance);
    }

    [TestMethod]
    public void SweepOnce_CallsReapVacantRooms_AndReturnsTheCount()
    {
        _repository.ReapVacantRooms(Arg.Any<TimeSpan>()).Returns(3);

        Assert.AreEqual(3, _service.SweepOnce());

        _repository.Received(1).ReapVacantRooms(TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void SweepOnce_WhenTheRepositoryThrows_ReturnsZeroAndDoesNotPropagate()
    {
        _repository.ReapVacantRooms(Arg.Any<TimeSpan>()).Returns(_ => throw new InvalidOperationException("boom"));

        Assert.AreEqual(0, _service.SweepOnce());
    }

    [TestMethod]
    public async Task StartAsync_ThenStopAsync_CompletesTheExecuteTaskWithoutFaulting()
    {
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(CancellationToken.None);

        Assert.IsNotNull(_service.ExecuteTask);
        Assert.IsTrue(_service.ExecuteTask.IsCompleted);
        Assert.IsFalse(_service.ExecuteTask.IsFaulted);
    }
}
