namespace Casino.Domain.Enums;

public enum PlayerStatus
{
    ACTIVE,
    INACTIVE,
    BLOCKED
}

public enum BrandStatus
{
    ACTIVE,
    INACTIVE
}

public enum OperatorStatus
{
    ACTIVE,
    INACTIVE
}

public enum GameSessionStatus
{
    OPEN,
    CLOSED,
    EXPIRED
}

public enum RoundStatus
{
    OPEN,
    CLOSED,
    CANCELLED
}

public enum BackofficeUserRole
{
    SUPER_ADMIN,
    OPERATOR_ADMIN,
    CASHIER
}

public enum BackofficeUserStatus
{
    ACTIVE,
    INACTIVE
}