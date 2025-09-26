namespace Casino.Application.DTOs.Gateway;

public record BalanceGatewayRequest(string SessionId, string PlayerId);

public record BalanceGatewayResponse(long Balance);

public record BetRequest(
    string SessionId,
    string PlayerId,
    string RoundId,
    long Amount,
    string TxId);

public record WinRequest(
    string SessionId,
    string PlayerId,
    string RoundId,
    long Amount,
    string TxId);

public record RollbackGatewayRequest(
    string SessionId,
    string TxIdOriginal);

public record CloseRoundRequest(
    string SessionId,
    string RoundId);

public record GatewayResponse(
    bool Ok,
    long Balance,
    string? ErrorMessage = null);