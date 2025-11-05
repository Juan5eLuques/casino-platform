using Casino.Application.Abstractions;
using Casino.Application.Features.Transactions;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Transactions;

public class TransferHandler : IRequestHandler<TransferCommand, TransferResult>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public TransferHandler(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransferResult> Handle(TransferCommand req, CancellationToken ct)
    {
        if (_currentUser.Id is null)
            return new TransferResult(false, "Usuario no autenticado");

        if (req.Amount <= 0)
            return new TransferResult(false, "El monto debe ser mayor a cero");

        var fromUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct);
        if (fromUser == null)
            return new TransferResult(false, "Usuario origen no encontrado");

        if (fromUser.Balance < req.Amount)
            return new TransferResult(false, "Saldo insuficiente");

        var toUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.ToUserId, ct);
        if (toUser == null)
            return new TransferResult(false, "Usuario destino no encontrado");

        fromUser.Balance -= req.Amount;
        toUser.Balance += req.Amount;
        fromUser.UpdatedAt = DateTime.UtcNow;
        toUser.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new TransferResult(true);
    }
}
