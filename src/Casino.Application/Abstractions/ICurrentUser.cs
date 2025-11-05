namespace Casino.Application.Abstractions;

public interface ICurrentUser
{
    int? Id { get; }
    string? Email { get; }
    string[] Roles { get; }
}
