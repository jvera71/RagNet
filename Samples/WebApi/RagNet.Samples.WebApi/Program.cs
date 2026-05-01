using RagNet;
using RagNet.Abstractions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// STEP 1: Configure .NET Aspire defaults.
// This automatically adds resilience, health checks, service discovery,
// and the base OpenTelemetry configuration (Metrics, Tracing, and Logs).
builder.AddServiceDefaults();

// Add services for OpenAPI (Swagger) documentation generation.
builder.Services.AddOpenApi();

// STEP 2: Integrate RagNet Telemetry.
// We expand the OpenTelemetry configuration to include RagNet-specific traces.
// This will allow visualizing the internal RagNet flow (pipeline) in the Aspire Dashboard.
builder.Services.AddOpenTelemetry().WithTracing(tracing =>
{
    // Registers the ActivitySources defined within RagNet.
    tracing.AddRagNetInstrumentation();
});

// STEP 3: Configure and inject RagNet.
// AddAdvancedRag registers all core RagNet services into the dependency injection container.
builder.Services.AddAdvancedRag(rag =>
{
    // We register a named pipeline (in this case "default").
    // A pipeline orchestrates query transformation, retrieval, and generation.
    rag.AddPipeline("default", pipeline =>
    {
        // In a real-world implementation, we would add components here such as:
        // pipeline.UseQueryTransformation<MyTransformer>();
        // pipeline.UseSemanticKernelGenerator();
        
        // For this example, we use a basic middleware that simply passes context to the next step.
        // This allows the pipeline to build correctly without requiring LLM credentials.
        pipeline.Use((ctx, next) => next(ctx));
    });
});

var app = builder.Build();

// Maps the default Aspire endpoints (e.g. /health, /alive).
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline for the development environment.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// STEP 4: Expose the Streaming RAG endpoint.
// We inject `IRagPipeline`, which was automatically configured by `AddAdvancedRag`.
app.MapPost("/api/rag/stream", (IRagPipeline pipeline, [FromBody] RagQueryRequest request) =>
{
    // ExecuteStreamingAsync runs the pipeline and returns an IAsyncEnumerable<StreamingRagResponse>.
    // ASP.NET Core natively handles this type to stream the HTTP response
    // in chunked format to the client as the model generates tokens.
    return pipeline.ExecuteStreamingAsync(request.Query);
})
.WithName("StreamRagResponse");

app.Run();

/// <summary>
/// Input model for the RAG request.
/// </summary>
public class RagQueryRequest
{
    public string Query { get; set; } = string.Empty;
}
