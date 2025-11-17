using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Casino.Api.Filters;

/// <summary>
/// Operation filter to handle file uploads in Minimal APIs for Swagger documentation
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
     // Check if the endpoint has HttpRequest parameter (indicates file upload)
        var hasHttpRequest = context.MethodInfo.GetParameters()
 .Any(p => p.ParameterType == typeof(HttpRequest));

        if (!hasHttpRequest)
return;

// Check if the endpoint path contains upload
        var isUploadEndpoint = context.ApiDescription.RelativePath?.Contains("upload") ?? false;

        if (!isUploadEndpoint)
          return;

 // Configure request body for file upload
 operation.RequestBody = new OpenApiRequestBody
        {
          Required = true,
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
      Format = "binary",
Description = "The file to upload (max 5MB, formats: JPG, PNG, GIF, WebP, SVG)"
   }
               },
            Required = new HashSet<string> { "file" }
           }
      }
            }
 };

  // Remove any auto-generated parameters that conflict
      var parametersToRemove = operation.Parameters
 .Where(p => p.In == ParameterLocation.Query || p.Name == "request")
.ToList();

  foreach (var param in parametersToRemove)
        {
         operation.Parameters.Remove(param);
     }
    }
}
