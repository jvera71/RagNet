using RagNet.Abstractions;

namespace RagNet.Core.Pipeline;

/// <summary>
/// Delegate that represents a step in the RAG pipeline.
/// Each step receives the context and can invoke the next step.
/// </summary>
public delegate Task<RagResponse> RagPipelineDelegate(RagPipelineContext context);
