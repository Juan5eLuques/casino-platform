namespace Casino.Application.Abstractions;

public interface IDateTime
{
    DateTime UtcNow { get; }
}