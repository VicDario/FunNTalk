using FunNTalk.API.Extensions;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.UseCases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FunNTalk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommunicationController(
    IGetUsersFromRoomUseCase getUsersFromRoomUseCase,
    IGetIceServersUseCase getIceServersUseCase,
    ICreateRoomUseCase createRoomUseCase) : ControllerBase
{
    private readonly IGetUsersFromRoomUseCase _getUsersFromRoomUseCase = getUsersFromRoomUseCase;
    private readonly IGetIceServersUseCase _getIceServersUseCase = getIceServersUseCase;
    private readonly ICreateRoomUseCase _createRoomUseCase = createRoomUseCase;

    [HttpGet]
    [Route("room/{roomName}/participants")]
    public IActionResult GetUsersFromRoom([FromRoute] string roomName)
    {
        var users = _getUsersFromRoomUseCase.Execute(roomName);

        if (users == null) return NotFound(new { Message = $"No users found for room {roomName}." });

        return Ok(users);
    }

    [HttpPost]
    [Route("rooms")]
    [EnableRateLimiting(RateLimitingExtension.CreateRoomPolicy)]
    public IActionResult CreateRoom()
    {
        var code = _createRoomUseCase.Execute();

        if (code is null)
        {
            return StatusCode(503, new { Message = "Could not allocate a room code. Try again." });
        }

        return Ok(new RoomDto(code));
    }

    /// <summary>
    /// The client fetches this once per session to configure its RTCPeerConnection. On failure
    /// it falls back to STUN only, which leaves peers behind symmetric NAT unable to connect —
    /// so this always answers 200 and degrades to STUN rather than returning an error.
    /// </summary>
    [HttpGet]
    [Route("ice-servers")]
    [EnableRateLimiting(RateLimitingExtension.IceServersPolicy)]
    public async Task<IActionResult> GetIceServers(CancellationToken cancellationToken)
    {
        var iceServers = await _getIceServersUseCase.ExecuteAsync(cancellationToken);
        return Ok(iceServers);
    }
}
