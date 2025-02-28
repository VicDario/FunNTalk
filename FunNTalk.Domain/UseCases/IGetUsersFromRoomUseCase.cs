using FunNTalk.Domain.Entities;

namespace FunNTalk.Domain.UseCases;

public interface IGetUsersFromRoomUseCase
{
    List<UserEntity>? Execute(string connectionId);
}
