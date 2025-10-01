using Casino.Application.DTOs.Admin;

namespace Casino.Application.Services;

public interface IBackofficeUserService
{
    Task<GetBackofficeUserResponse> CreateUserAsync(CreateBackofficeUserRequest request, Guid currentUserId);
    Task<QueryBackofficeUsersResponse> GetUsersAsync(QueryBackofficeUsersRequest request, Guid? operatorScope = null);
    Task<GetBackofficeUserResponse?> GetUserAsync(Guid userId, Guid? operatorScope = null);
    Task<GetBackofficeUserResponse> UpdateUserAsync(Guid userId, UpdateBackofficeUserRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<bool> DeleteUserAsync(Guid userId, Guid currentUserId, Guid? operatorScope = null);
}