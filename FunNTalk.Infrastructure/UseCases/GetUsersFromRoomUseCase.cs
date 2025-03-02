using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.UseCases;

namespace FunNTalk.Infrastructure.UseCases;

class GetUsersFromRoomUseCase(IChatRoomRepository chatRoomRepository) : IGetUsersFromRoomUseCase
{
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public List<UserDto>? Execute(string roomName)
    {
        var room = _chatRoomRepository.GetRoom(roomName);
        return (List<UserDto>?)(room?.Participants.Select(UserDto.FromEntity) ?? null);
    }
}
