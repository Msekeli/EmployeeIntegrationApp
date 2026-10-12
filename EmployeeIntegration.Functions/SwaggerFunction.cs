using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EmployeeIntegration.Functions;

public sealed class SwaggerFunction
{
    private const string ResourcePrefix = "Swashbuckle.AspNetCore.SwaggerUI.node_modules.swagger_ui_dist.";
    private static readonly HashSet<string> UiAssets = new(StringComparer.Ordinal)
    {
        "swagger-ui-bundle.js",
        "swagger-ui-standalone-preset.js",
        "swagger-ui.css",
        "favicon-32x32.png"
    };

    [Function("OpenApiDocument")]
    public async Task<HttpResponseData> GetOpenApiDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequestData request)
    {
        var document = typeof(SwaggerFunction).Assembly
            .GetManifestResourceStream("EmployeeIntegration_Functions.openapi.json");
        if (document is null)
            return request.CreateResponse(HttpStatusCode.NotFound);

        using (document)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await document.CopyToAsync(response.Body);
            return response;
        }
    }

    [Function("SwaggerUi")]
    public async Task<HttpResponseData> GetSwaggerUi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger")] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        await response.WriteStringAsync("""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Employee Integration API — Swagger UI</title>
              <link rel="stylesheet" href="/api/swagger/swagger-ui.css" />
            </head>
            <body>
              <div id="swagger-ui"></div>
              <script src="/api/swagger/swagger-ui-bundle.js"></script>
              <script src="/api/swagger/swagger-ui-standalone-preset.js"></script>
              <script>
                window.onload = () => SwaggerUIBundle({
                  url: "/api/openapi.json",
                  dom_id: "#swagger-ui",
                  presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                  layout: "StandaloneLayout"
                });
              </script>
            </body>
            </html>
            """);
        return response;
    }

    [Function("SwaggerUiAsset")]
    public async Task<HttpResponseData> GetSwaggerUiAsset(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/{asset}")] HttpRequestData request,
        string asset)
    {
        if (!UiAssets.Contains(asset))
            return request.CreateResponse(HttpStatusCode.NotFound);

        var stream = typeof(SwaggerUIOptions).Assembly.GetManifestResourceStream(ResourcePrefix + asset);
        if (stream is null)
            return request.CreateResponse(HttpStatusCode.NotFound);

        using (stream)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", asset.EndsWith(".css", StringComparison.Ordinal)
                ? "text/css; charset=utf-8"
                : asset.EndsWith(".js", StringComparison.Ordinal)
                    ? "application/javascript; charset=utf-8"
                    : "image/png");
            await stream.CopyToAsync(response.Body);
            return response;
        }
    }
}
