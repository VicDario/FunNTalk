using System.Security.Cryptography;
using FunNTalk.Domain.Extensions;
using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.UseCases;
using FunNTalk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FunNTalk.Infrastructure.UseCases;

/// <summary>
/// Owns the RNG and the retry bound. The retry loop lives here, outside the repository's lock:
/// the only compound operation that needs the lock is the atomic contains-then-insert inside
/// <see cref="IChatRoomRepository.TryCreateRoom"/>, and generating a candidate is pure — holding
/// the registry lock across N cryptographic RNG draws would block every concurrent hub
/// invocation for no gain.
/// </summary>
internal sealed class CreateRoomUseCase(IChatRoomRepository repository, IOptions<RoomOptions> options)
    : ICreateRoomUseCase
{
    private readonly IChatRoomRepository _repository = repository;
    private readonly RoomOptions _options = options.Value;

    public string? Execute()
    {
        for (var attempt = 0; attempt < _options.MaxMintAttempts; attempt++)
        {
            var candidate = Generate();
            if (_repository.TryCreateRoom(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    /// <see cref="RandomNumberGenerator.GetItems{T}"/>'s static form selects uniformly with its
    /// own rejection sampling — no modulo bias, and no instance to accidentally cache and share
    /// across concurrent requests (the instance returned by <see cref="RandomNumberGenerator.Create"/>
    /// is not documented thread-safe; the statics are).
    /// </summary>
    private static string Generate() =>
        new(RandomNumberGenerator.GetItems<char>(RoomCodeExtensions.Alphabet, RoomCodeExtensions.Length));
}
