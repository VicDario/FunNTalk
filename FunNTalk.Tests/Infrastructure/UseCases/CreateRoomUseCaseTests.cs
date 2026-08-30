using FunNTalk.Domain.Extensions;
using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Configuration;
using FunNTalk.Infrastructure.UseCases;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.UseCases;

[TestClass]
public class CreateRoomUseCaseTests
{
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();

    private CreateRoomUseCase CreateUseCase(RoomOptions? options = null) =>
        new(_repository, Options.Create(options ?? new RoomOptions()));

    [TestMethod]
    public void Execute_GeneratesAWellFormedCode_AndReservesIt()
    {
        _repository.TryCreateRoom(Arg.Any<string>()).Returns(true);
        var useCase = CreateUseCase();

        var code = useCase.Execute();

        Assert.IsNotNull(code);
        Assert.IsTrue(code.IsWellFormedRoomCode());
        _repository.Received(1).TryCreateRoom(code);
    }

    [TestMethod]
    public void Execute_RetriesOnCollision_UpToTheBoundThenReturnsNull()
    {
        _repository.TryCreateRoom(Arg.Any<string>()).Returns(false);
        var useCase = CreateUseCase(new RoomOptions { MaxMintAttempts = 5 });

        var code = useCase.Execute();

        Assert.IsNull(code);
        _repository.Received(5).TryCreateRoom(Arg.Any<string>());
    }

    [TestMethod]
    public void Execute_GeneratesOnly_WellFormedCodes()
    {
        _repository.TryCreateRoom(Arg.Any<string>()).Returns(true);
        var useCase = CreateUseCase();

        for (var iteration = 0; iteration < 1000; iteration++)
        {
            var code = useCase.Execute();

            Assert.IsNotNull(code);
            Assert.AreEqual(RoomCodeExtensions.Length, code.Length);
            Assert.IsTrue(code.IsWellFormedRoomCode());
            Assert.IsFalse(code.Contains('I') || code.Contains('L') || code.Contains('O') || code.Contains('U'));
        }
    }
}
