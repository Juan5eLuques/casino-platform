using Casino.Application.Abstractions;
using Casino.Application.Features.Transactions;
using Casino.Domain.Users;
using Casino.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Casino.Infrastructure.Features.Transactions;

public class LoadChipsHandler : IRequestHandler<LoadChipsCommand, TransferResult>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LoadChipsHandler(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransferResult> Handle(LoadChipsCommand req, CancellationToken ct)
    {
        if (_currentUser.Id is null)
            return new TransferResult(false, "Usuario no autenticado");

        var cashier = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct);
        if (cashier == null || (cashier.Role != Role.CASHIER && cashier.Role != Role.SUPERADMIN && cashier.Role != Role.ADMIN))
            return new TransferResult(false, "Solo un cajero, admin o superadmin puede cargar fichas");

        var toUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.ToUserId, ct);
        if (toUser == null)
            return new TransferResult(false, "Usuario destino no encontrado");

        // Validar jerarquía: el usuario debe ser subordinado directo o indirecto
        if (!await IsUserInHierarchy(cashier.Id, toUser.Id, ct))
            return new TransferResult(false, "No tiene permisos para cargar fichas a este usuario");

        if (cashier.Balance < req.Amount)
            return new TransferResult(false, "Saldo insuficiente en el cajero");

        cashier.Balance -= req.Amount;
        toUser.Balance += req.Amount;
        cashier.UpdatedAt = DateTime.UtcNow;
        toUser.UpdatedAt = DateTime.UtcNow;

        _db.Transfers.Add(new Transfer {
            FromUserId = cashier.Id,
            ToUserId = toUser.Id,
            Amount = req.Amount,
            Date = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return new TransferResult(true);
    }

    private async Task<bool> IsUserInHierarchy(int parentUserId, int targetUserId, CancellationToken ct)
    {
        // SUPERADMIN puede cargar a cualquiera
        var parentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == parentUserId, ct);
        if (parentUser?.Role == Role.SUPERADMIN)
            return true;

        // Verificar si el usuario objetivo está en la jerarquía del padre (directo o indirecto)
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (targetUser == null) return false;

        // Buscar hacia arriba en la jerarquía del usuario objetivo
        var currentParentId = targetUser.ParentUserId;
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == parentUserId)
                return true;

            var currentParent = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentParentId.Value, ct);
            currentParentId = currentParent?.ParentUserId;
        }

        return false;
    }
}
