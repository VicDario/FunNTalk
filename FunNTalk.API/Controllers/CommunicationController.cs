using FunNTalk.Domain.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FunNTalk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommunicationController(IGetUsersFromRoomUseCase getUsersFromRoomUseCase) : ControllerBase
{
    private readonly IGetUsersFromRoomUseCase _getUsersFromRoomUseCase = getUsersFromRoomUseCase;

    [HttpGet]
    [Route("room/{roomName}/participants")]
    public IActionResult GetUsersFromRoom([FromRoute] string roomName)
    {
        var users = _getUsersFromRoomUseCase.Execute(roomName);

        if (users == null) return NotFound(new { Message = $"No users found for room {roomName}." });

        return Ok(users);
    }
}
