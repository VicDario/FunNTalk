namespace FunNTalk.Domain.UseCases;

public interface ICreateRoomUseCase
{
    /// <summary>Mints and reserves an unused room code, or null if the retry bound is exhausted.</summary>
    string? Execute();
}
