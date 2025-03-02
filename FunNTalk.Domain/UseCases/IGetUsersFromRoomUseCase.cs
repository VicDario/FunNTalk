using FunNTalk.Domain.DTOs;

namespace FunNTalk.Domain.UseCases;

public interface IGetUsersFromRoomUseCase
{
    List<UserDto>? Execute(string connectionId);
}
