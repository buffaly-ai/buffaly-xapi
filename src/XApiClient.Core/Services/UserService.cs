using XApiClient.Core.Models;

namespace XApiClient.Core.Services;

public sealed class UserService : IUserService
{
    private readonly XClient _client;

    public UserService(XClient client)
    {
        _client = client;
    }

    public Task<XResponse<User>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMeAsync(cancellationToken);
    }
}
