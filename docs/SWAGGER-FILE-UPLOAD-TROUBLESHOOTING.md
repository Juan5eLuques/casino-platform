# ?? Swagger File Upload Troubleshooting

## Problema

Swagger UI no envía archivos correctamente a endpoints de Minimal APIs que usan `HttpRequest` para acceder a archivos.

## Síntomas

- Seleccionas un archivo en Swagger UI
- Click "Execute"
- Recibes: `"File is required. Make sure the field name is 'file'"`

## Causa Raíz

Swagger UI genera automáticamente el nombre del campo del archivo, y no siempre usa "file" como nombre. En algunos casos puede usar nombres aleatorios o diferentes según la configuración de Swashbuckle.

## Solución Implementada

### 1. Detección Flexible de Nombres de Campo

El código ahora intenta múltiples nombres de campo comunes:

```csharp
var possibleNames = new[] { "file", "File", "files", "Files", "upload", "image" };
foreach (var name in possibleNames)
{
    uploadedFile = request.Form.Files.GetFile(name);
    if (uploadedFile != null) break;
}

// Fallback: tomar el primer archivo sin importar el nombre
if (uploadedFile == null && request.Form.Files.Count > 0)
{
    uploadedFile = request.Form.Files[0];
}
```

### 2. Logging Detallado

Cada request ahora loguea:
- Número de archivos recibidos
- Nombre del campo de cada archivo
- Nombre del archivo
- Tamaño del archivo

```csharp
logger.LogInformation("Form has {Count} files", request.Form.Files.Count);
foreach (var file in request.Form.Files)
{
    logger.LogInformation("File field name: {Name}, FileName: {FileName}, Size: {Size}", 
        file.Name, file.FileName, file.Length);
}
```

### 3. Mensajes de Error Informativos

Si falla, el error ahora incluye los nombres de campos disponibles:

```csharp
var errorMsg = request.HasFormContentType 
    ? $"No file found. Available fields: {string.Join(", ", request.Form.Files.Select(f => f.Name))}"
    : "Invalid content type. Expected multipart/form-data";
```

## Cómo Diagnosticar

### Paso 1: Revisar Logs del Servidor

Cuando hagas un upload desde Swagger, revisa la consola de la aplicación. Deberías ver:

```
info: Form has 1 files
info: File field name: formFile, FileName: banner.jpg, Size: 123456
info: Found file with field name: formFile
```

Esto te dice exactamente qué nombre de campo está usando Swagger.

### Paso 2: Si Swagger Usa un Nombre Diferente

Si ves que Swagger usa un nombre diferente (ej: "formFile", "attachment"), agrega ese nombre al array de nombres posibles:

```csharp
var possibleNames = new[] { 
    "file", "File", "files", "Files", 
    "upload", "image",
    "formFile",  // Agregar aquí
"attachment" // Agregar aquí
};
```

### Paso 3: Última Opción - Fallback Funciona Siempre

Como el código tiene un fallback que toma el primer archivo sin importar el nombre, **debería funcionar siempre**, incluso si Swagger usa un nombre completamente aleatorio.

## Alternativas si Swagger Sigue Fallando

### Opción 1: Usar Controller en lugar de Minimal API

Si Swagger sigue sin funcionar, considera crear un Controller tradicional solo para file uploads:

```csharp
[ApiController]
[Route("api/v1/admin/brands/assets")]
public class BrandAssetsController : ControllerBase
{
    [HttpPost("upload/banner/{section}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadBanner(
        string section,
        [FromForm] IFormFile file)  // ? Esto funciona perfectamente en Controllers
    {
 // ...
 }
}
```

Controllers tradicionales tienen mejor soporte de Swagger para file uploads.

### Opción 2: Usar Postman/Insomnia en lugar de Swagger

Para operaciones de file upload, usa herramientas dedicadas:

**Postman:**
```
POST /api/v1/admin/brands/assets/upload/banner/home
Body: form-data
Key: file (Type: File)
Value: [Select File]
```

**Insomnia:**
```
POST /api/v1/admin/brands/assets/upload/banner/home
Body: Multipart Form
Name: file
Type: File
File: [Select File]
```

### Opción 3: cURL (Más Confiable)

```bash
curl -X POST "https://localhost:5001/api/v1/admin/brands/assets/upload/banner/home" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Host: your-brand.com" \
  -F "file=@banner.jpg"
```

cURL siempre funciona porque especificas explícitamente el nombre del campo.

## Configuración Adicional de Swagger (Opcional)

Si quieres que Swagger funcione mejor con file uploads, agrega esta configuración en `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(c =>
{
    // Configuración existente...
    
    // Agregar soporte mejorado para file uploads
    c.OperationFilter<FileUploadOperationFilter>();
});

// Crear el filtro
public class FileUploadOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
   var fileParams = context.MethodInfo.GetParameters()
        .Where(p => p.ParameterType == typeof(IFormFile) || 
                 p.ParameterType == typeof(IFormFileCollection));

        if (fileParams.Any())
        {
         operation.RequestBody = new OpenApiRequestBody
  {
         Content = new Dictionary<string, OpenApiMediaType>
    {
         ["multipart/form-data"] = new OpenApiMediaType
      {
   Schema = new OpenApiSchema
         {
      Type = "object",
     Properties = new Dictionary<string, OpenApiSchema>
        {
            ["file"] = new OpenApiSchema
             {
             Type = "string",
   Format = "binary"
                  }
            }
       }
      }
        }
            };
        }
    }
}
```

## Estado Actual

? **Solución Implementada**: El código ahora acepta cualquier nombre de campo y tiene un fallback que toma el primer archivo.

? **Logging Detallado**: Puedes ver exactamente qué está enviando Swagger.

? **Debería Funcionar**: Con el fallback, incluso si Swagger usa un nombre diferente, el archivo se detectará.

## Próximos Pasos

1. **Reiniciar la aplicación** (importante para aplicar cambios)
2. **Probar en Swagger** y revisar logs
3. **Si falla, usar Postman o cURL** como alternativa
4. **Reportar el nombre de campo** que usa Swagger para agregarlo al código

## Referencias

- [ASP.NET Core File Uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
- [Swashbuckle File Upload Issues](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2303)
- [Minimal APIs File Upload](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding)

---

**Fecha**: 2025-01-13  
**Status**: ? Fix aplicado con fallback flexible
