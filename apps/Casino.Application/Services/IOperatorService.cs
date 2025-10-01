using Casino.Application.DTOs.Operator;

namespace Casino.Application.Services;

public interface IOperatorService
{
    Task<GetOperatorResponse> CreateOperatorAsync(CreateOperatorRequest request, Guid currentUserId);
    Task<QueryOperatorsResponse> GetOperatorsAsync(QueryOperatorsRequest request, Guid? operatorScope = null);
    Task<GetOperatorResponse?> GetOperatorAsync(Guid operatorId, Guid? operatorScope = null);
    Task<GetOperatorResponse> UpdateOperatorAsync(Guid operatorId, UpdateOperatorRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<bool> DeleteOperatorAsync(Guid operatorId, Guid currentUserId, Guid? operatorScope = null);
}