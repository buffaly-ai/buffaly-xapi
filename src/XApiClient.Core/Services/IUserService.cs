using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public interface IUserService
{
    Task<XResponse<User>> GetMeAsync(CancellationToken cancellationToken = default);
}
