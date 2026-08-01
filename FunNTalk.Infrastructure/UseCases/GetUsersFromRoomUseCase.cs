using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.UseCases;

namespace FunNTalk.Infrastructure.UseCases;

class GetUsersFromRoomUseCase(IChatRoomRepository chatRoomRepository) : IGetUsersFromRoomUseCase
{
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public List<UserDto>? Execute(string roomName)
    {
        var participants = _chatRoomRepository.GetParticipants(roomName);
        return participants?.Select(UserDto.FromEntity).ToList();
    }
}
