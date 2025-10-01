using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Operator;

// Request DTOs
public record CreateOperatorRequest(
    string Name,
    OperatorStatus Status = OperatorStatus.ACTIVE);

public record UpdateOperatorRequest(
    string? Name = null,
    OperatorStatus? Status = null);

public record QueryOperatorsRequest(
    string? Name = null,
    OperatorStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

// Response DTOs
public record GetOperatorResponse(
    Guid Id,
    string Name,
    OperatorStatus Status,
    DateTime CreatedAt,
    int BrandCount);

public record QueryOperatorsResponse(
    IEnumerable<GetOperatorResponse> Operators,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);