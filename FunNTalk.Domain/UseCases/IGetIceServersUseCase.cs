using FunNTalk.Domain.DTOs;

namespace FunNTalk.Domain.UseCases;

public interface IGetIceServersUseCase
{
    Task<IReadOnlyList<IceServerDto>> ExecuteAsync(CancellationToken cancellationToken = default);
}
