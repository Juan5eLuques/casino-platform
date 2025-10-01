using Casino.Application.DTOs.Operator;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class OperatorService : IOperatorService
{
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<OperatorService> _logger;

    public OperatorService(
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<OperatorService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetOperatorResponse> CreateOperatorAsync(CreateOperatorRequest request, Guid currentUserId)
    {
        // Verificar que el nombre no esté en uso
        var existingOperator = await _context.Operators
            .FirstOrDefaultAsync(o => o.Name == request.Name);

        if (existingOperator != null)
        {
            throw new InvalidOperationException($"Operator with name '{request.Name}' already exists");
        }

        var newOperator = new Operator
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Operators.Add(newOperator);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "CREATE_OPERATOR",
            "Operator",
            newOperator.Id.ToString(),
            new { request.Name, request.Status });

        _logger.LogInformation("Operator created: {OperatorId} - {Name} by user {UserId}",
            newOperator.Id, newOperator.Name, currentUserId);

        return new GetOperatorResponse(
            newOperator.Id,
            newOperator.Name,
            newOperator.Status,
            newOperator.CreatedAt,
            0); // Nuevo operador no tiene marcas aún
    }

    public async Task<QueryOperatorsResponse> GetOperatorsAsync(QueryOperatorsRequest request, Guid? operatorScope = null)
    {
        var query = _context.Operators.AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue)
        {
            query = query.Where(o => o.Id == operatorScope.Value);
        }

        // Aplicar filtros
        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(o => o.Name.Contains(request.Name));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var operators = await query
            .Include(o => o.Brands)
            .OrderBy(o => o.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new GetOperatorResponse(
                o.Id,
                o.Name,
                o.Status,
                o.CreatedAt,
                o.Brands.Count))
            .ToListAsync();

        return new QueryOperatorsResponse(
            operators,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }

    public async Task<GetOperatorResponse?> GetOperatorAsync(Guid operatorId, Guid? operatorScope = null)
    {
        var query = _context.Operators.AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue && operatorScope.Value != operatorId)
        {
            return null; // No puede ver otros operadores
        }

        var operatorEntity = await query
            .Include(o => o.Brands)
            .FirstOrDefaultAsync(o => o.Id == operatorId);

        if (operatorEntity == null)
            return null;

        return new GetOperatorResponse(
            operatorEntity.Id,
            operatorEntity.Name,
            operatorEntity.Status,
            operatorEntity.CreatedAt,
            operatorEntity.Brands.Count);
    }

    public async Task<GetOperatorResponse> UpdateOperatorAsync(Guid operatorId, UpdateOperatorRequest request, Guid currentUserId, Guid? operatorScope = null)
    {
        var query = _context.Operators.AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue && operatorScope.Value != operatorId)
        {
            throw new InvalidOperationException("Access denied: cannot update this operator");
        }

        var operatorEntity = await query
            .Include(o => o.Brands)
            .FirstOrDefaultAsync(o => o.Id == operatorId);

        if (operatorEntity == null)
        {
            throw new InvalidOperationException("Operator not found");
        }

        var changes = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(request.Name) && request.Name != operatorEntity.Name)
        {
            // Verificar que el nuevo nombre no esté en uso
            var existingOperator = await _context.Operators
                .FirstOrDefaultAsync(o => o.Name == request.Name && o.Id != operatorId);

            if (existingOperator != null)
            {
                throw new InvalidOperationException($"Operator with name '{request.Name}' already exists");
            }

            changes["Name"] = new { Old = operatorEntity.Name, New = request.Name };
            operatorEntity.Name = request.Name;
        }

        if (request.Status.HasValue && request.Status.Value != operatorEntity.Status)
        {
            changes["Status"] = new { Old = operatorEntity.Status, New = request.Status.Value };
            operatorEntity.Status = request.Status.Value;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_OPERATOR",
                "Operator",
                operatorEntity.Id.ToString(),
                changes);

            _logger.LogInformation("Operator updated: {OperatorId} by user {UserId}",
                operatorEntity.Id, currentUserId);
        }

        return new GetOperatorResponse(
            operatorEntity.Id,
            operatorEntity.Name,
            operatorEntity.Status,
            operatorEntity.CreatedAt,
            operatorEntity.Brands.Count);
    }

    public async Task<bool> DeleteOperatorAsync(Guid operatorId, Guid currentUserId, Guid? operatorScope = null)
    {
        // Solo SUPER_ADMIN puede eliminar operadores
        if (operatorScope.HasValue)
        {
            throw new InvalidOperationException("Access denied: only SUPER_ADMIN can delete operators");
        }

        var operatorEntity = await _context.Operators
            .Include(o => o.Brands)
            .ThenInclude(b => b.Players)
            .FirstOrDefaultAsync(o => o.Id == operatorId);

        if (operatorEntity == null)
            return false;

        // Verificar si tiene marcas activas con jugadores
        var hasActiveBrands = operatorEntity.Brands.Any(b => b.Status == BrandStatus.ACTIVE);
        var hasPlayers = operatorEntity.Brands.Any(b => b.Players.Any());

        if (hasActiveBrands || hasPlayers)
        {
            throw new InvalidOperationException("Cannot delete operator with active brands or players. Deactivate all brands first.");
        }

        _context.Operators.Remove(operatorEntity);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "DELETE_OPERATOR",
            "Operator",
            operatorEntity.Id.ToString(),
            new { operatorEntity.Name, DeletedAt = DateTime.UtcNow });

        _logger.LogInformation("Operator deleted: {OperatorId} by user {UserId}",
            operatorEntity.Id, currentUserId);

        return true;
    }
}