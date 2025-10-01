using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class BackofficeUserService : IBackofficeUserService
{
    private readonly CasinoDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackofficeUserService> _logger;

    public BackofficeUserService(
        CasinoDbContext context,
        IPasswordService passwordService,
        IAuditService auditService,
        ILogger<BackofficeUserService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetBackofficeUserResponse> CreateUserAsync(CreateBackofficeUserRequest request, Guid currentUserId)
    {
        // Verificar que el username no esté en uso
        var existingUser = await _context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists");
        }

        // Validar operatorId según el rol
        if (request.Role == BackofficeUserRole.SUPER_ADMIN && request.OperatorId.HasValue)
        {
            throw new InvalidOperationException("SUPER_ADMIN cannot be assigned to a specific operator");
        }

        if (request.Role != BackofficeUserRole.SUPER_ADMIN && !request.OperatorId.HasValue)
        {
            throw new InvalidOperationException($"{request.Role} must be assigned to an operator");
        }

        // Verificar que el operador existe si se especifica
        if (request.OperatorId.HasValue)
        {
            var operatorExists = await _context.Operators
                .AnyAsync(o => o.Id == request.OperatorId.Value && o.Status == OperatorStatus.ACTIVE);

            if (!operatorExists)
            {
                throw new InvalidOperationException("Operator not found or inactive");
            }
        }

        var passwordHash = _passwordService.HashPassword(request.Password);

        var newUser = new BackofficeUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = passwordHash,
            Role = request.Role,
            OperatorId = request.OperatorId,
            Status = BackofficeUserStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow
        };

        _context.BackofficeUsers.Add(newUser);
        await _context.SaveChangesAsync();

        // Cargar el operador para la respuesta
        await _context.Entry(newUser)
            .Reference(u => u.Operator)
            .LoadAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "CREATE_USER",
            "BackofficeUser",
            newUser.Id.ToString(),
            new { 
                request.Username, 
                request.Role, 
                OperatorId = request.OperatorId,
                OperatorName = newUser.Operator?.Name 
            });

        _logger.LogInformation("Backoffice user created: {UserId} - {Username} - {Role} by user {CreatedByUserId}",
            newUser.Id, newUser.Username, newUser.Role, currentUserId);

        return new GetBackofficeUserResponse(
            newUser.Id,
            newUser.Username,
            newUser.Role,
            newUser.Status,
            newUser.OperatorId,
            newUser.Operator?.Name,
            newUser.CreatedAt,
            newUser.LastLoginAt);
    }

    public async Task<QueryBackofficeUsersResponse> GetUsersAsync(QueryBackofficeUsersRequest request, Guid? operatorScope = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Operator)
            .AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue)
        {
            query = query.Where(u => u.OperatorId == operatorScope.Value);
        }

        // Aplicar filtros
        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(u => u.Username.Contains(request.Username));
        }

        if (request.Role.HasValue)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (request.OperatorId.HasValue)
        {
            query = query.Where(u => u.OperatorId == request.OperatorId.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new GetBackofficeUserResponse(
                u.Id,
                u.Username,
                u.Role,
                u.Status,
                u.OperatorId,
                u.Operator != null ? u.Operator.Name : null,
                u.CreatedAt,
                u.LastLoginAt))
            .ToListAsync();

        return new QueryBackofficeUsersResponse(
            users,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }

    public async Task<GetBackofficeUserResponse?> GetUserAsync(Guid userId, Guid? operatorScope = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Operator)
            .AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue)
        {
            query = query.Where(u => u.OperatorId == operatorScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new GetBackofficeUserResponse(
            user.Id,
            user.Username,
            user.Role,
            user.Status,
            user.OperatorId,
            user.Operator?.Name,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<GetBackofficeUserResponse> UpdateUserAsync(Guid userId, UpdateBackofficeUserRequest request, Guid currentUserId, Guid? operatorScope = null)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.Operator)
            .AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue)
        {
            query = query.Where(u => u.OperatorId == operatorScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found or access denied");
        }

        var changes = new Dictionary<string, object>();

        // Cambio de username
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            var existingUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != userId);

            if (existingUser != null)
            {
                throw new InvalidOperationException($"Username '{request.Username}' already exists");
            }

            changes["Username"] = new { Old = user.Username, New = request.Username };
            user.Username = request.Username;
        }

        // Cambio de password
        if (!string.IsNullOrEmpty(request.Password))
        {
            var newPasswordHash = _passwordService.HashPassword(request.Password);
            changes["Password"] = "Changed";
            user.PasswordHash = newPasswordHash;
        }

        // Cambio de rol
        if (request.Role.HasValue && request.Role.Value != user.Role)
        {
            // Validaciones específicas de rol
            if (request.Role.Value == BackofficeUserRole.SUPER_ADMIN && user.OperatorId.HasValue)
            {
                throw new InvalidOperationException("Cannot change user to SUPER_ADMIN while assigned to an operator");
            }

            changes["Role"] = new { Old = user.Role, New = request.Role.Value };
            user.Role = request.Role.Value;
        }

        // Cambio de status
        if (request.Status.HasValue && request.Status.Value != user.Status)
        {
            changes["Status"] = new { Old = user.Status, New = request.Status.Value };
            user.Status = request.Status.Value;
        }

        // Cambio de operador
        if (request.OperatorId != user.OperatorId)
        {
            if (user.Role == BackofficeUserRole.SUPER_ADMIN && request.OperatorId.HasValue)
            {
                throw new InvalidOperationException("SUPER_ADMIN cannot be assigned to a specific operator");
            }

            if (user.Role != BackofficeUserRole.SUPER_ADMIN && !request.OperatorId.HasValue)
            {
                throw new InvalidOperationException($"{user.Role} must be assigned to an operator");
            }

            if (request.OperatorId.HasValue)
            {
                var operatorExists = await _context.Operators
                    .AnyAsync(o => o.Id == request.OperatorId.Value && o.Status == OperatorStatus.ACTIVE);

                if (!operatorExists)
                {
                    throw new InvalidOperationException("Operator not found or inactive");
                }
            }

            changes["OperatorId"] = new { Old = user.OperatorId, New = request.OperatorId };
            user.OperatorId = request.OperatorId;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            // Recargar el operador si cambió
            await _context.Entry(user)
                .Reference(u => u.Operator)
                .LoadAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_USER",
                "BackofficeUser",
                user.Id.ToString(),
                changes);

            _logger.LogInformation("Backoffice user updated: {UserId} by user {UpdatedByUserId}",
                user.Id, currentUserId);
        }

        return new GetBackofficeUserResponse(
            user.Id,
            user.Username,
            user.Role,
            user.Status,
            user.OperatorId,
            user.Operator?.Name,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid currentUserId, Guid? operatorScope = null)
    {
        var query = _context.BackofficeUsers.AsQueryable();

        // Aplicar scope por operador si no es SUPER_ADMIN
        if (operatorScope.HasValue)
        {
            query = query.Where(u => u.OperatorId == operatorScope.Value);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        // No permitir eliminar al propio usuario
        if (user.Id == currentUserId)
        {
            throw new InvalidOperationException("Cannot delete your own user account");
        }

        // No permitir eliminar SUPER_ADMIN si eres OPERATOR_ADMIN
        if (user.Role == BackofficeUserRole.SUPER_ADMIN && operatorScope.HasValue)
        {
            throw new InvalidOperationException("Access denied: cannot delete SUPER_ADMIN users");
        }

        _context.BackofficeUsers.Remove(user);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "DELETE_USER",
            "BackofficeUser",
            user.Id.ToString(),
            new { user.Username, user.Role, DeletedAt = DateTime.UtcNow });

        _logger.LogInformation("Backoffice user deleted: {UserId} by user {DeletedByUserId}",
            user.Id, currentUserId);

        return true;
    }
}