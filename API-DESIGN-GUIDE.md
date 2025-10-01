# API Design Guide (Clean layering + Swagger sano)

## Objetivo
Evitar colisiones de esquemas en Swagger y mantener capas limpias.  
**Regla de oro:** La API solo expone **DTOs**; las capas internas nunca “salen” al HTTP directamente.

## Capas y Namespaces
- **Domain** (`Casino.Domain.*`): entidades y reglas de negocio.
- **Application** (`Casino.Application.*`):
  - **Services/Handlers** (lógica de casos de uso) → **nunca** expuestos como respuesta HTTP.
  - **DTOs** (`Casino.Application.DTOs.*`) → **únicos tipos** que salen/entran por HTTP.
  - **Mappers** (`Casino.Application.Mappers.*`) → conversión ServiceModel ⇄ DTO.
- **Infrastructure** (`Casino.Infrastructure.*`): persistencia, proveedores.
- **API** (`Casino.Api.*`): Minimal APIs / Controllers, configuración (Swagger, Auth, CORS, Filters).

## Naming
- Tipos internos de aplicación: `*Model` o `*Result` (p. ej. `GetBrandGameResult`).
- Tipos de transporte: `*Request`, `*Response`, `*Dto` dentro de `Casino.Application.DTOs.*`.
- Endpoints: **nunca** devolver `Services` o `Domain`; siempre `DTOs`.

## Swagger
En `Program.cs`:
```csharp
builder.Services.AddSwaggerGen(c =>
{
    // Evita choques de schemaId entre tipos con el mismo nombre en distintos namespaces
    c.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace('+', '.'));
});
```

## Patrón de endpoint (Minimal APIs)
```csharp
app.MapGet("/api/v1/admin/catalog/brands/{brandId:guid}/games",
    async (Guid brandId, IBrandCatalogQuery svc) =>
{
    var models = await svc.GetBrandGamesAsync(brandId); // IEnumerable<GetBrandGameResult>
    var dtos = models.Select(m => m.ToDto()); // IEnumerable<GetBrandGameResponse>
    return Results.Ok(dtos);
})
.Produces<IEnumerable<Casino.Application.DTOs.Game.GetBrandGameResponse>>(StatusCodes.Status200OK)
.WithName("AdminGetBrandGames")
.WithTags("Brand Admin");
```

## Mappers (sin AutoMapper)
```csharp
namespace Casino.Application.Mappers;

using Casino.Application.DTOs.Game;
using Casino.Application.Services.Models; // internos

public static class GameMappers
{
    public static GetBrandGameResponse ToDto(this GetBrandGameResult m) =>
        new GetBrandGameResponse(
            Code: m.Code,
            Provider: m.Provider,
            Name: m.Name,
            Enabled: m.Enabled,
            Tags: m.Tags
        );
}
```

## DTOs (solo en `Casino.Application.DTOs.*`)
```csharp
namespace Casino.Application.DTOs.Game;

public readonly record struct GetBrandGameResponse(
    string Code,
    string Provider,
    string Name,
    bool Enabled,
    IReadOnlyList<string> Tags
);
```

## Reglas de validación (FluentValidation)
- Validar **solo** DTOs de entrada (`*Request`/`*Dto`).
- Los servicios asumen entradas ya validadas (o validan invariantes de negocio adicionales).

## Produces / ProblemDetails
- Declarar `Produces<T>()` con **DTOs**.
- Errores con `ProblemDetails`. Nunca devolver modelos internos en errores.

## Checklist de PR
- [ ] Ningún endpoint expone tipos de `Services` o `Domain`.
- [ ] Todos los endpoints devuelven/aceptan **DTOs**.
- [ ] `CustomSchemaIds` configurado.
- [ ] Hay mapper(s) ServiceModel → DTO, y se usan.
- [ ] `Produces<TDto>()` presente en endpoints públicos.
