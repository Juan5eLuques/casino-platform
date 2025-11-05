using Casino.Application.Abstractions;

namespace Casino.Infrastructure.Clock;

public class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
