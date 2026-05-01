*   **Propósito:** Contiene la lógica principal del sistema RAG, la orquestación, los pipelines base y las implementaciones por defecto que no requieren librerías de terceros externas a Microsoft.
*   **Dependencias:** `RagNet.Abstractions`, `Microsoft.Extensions.AI`, `Microsoft.Extensions.VectorData`, `System.Diagnostics.DiagnosticSource` (para OpenTelemetry).
*   **Componentes del diseño:**
    *   **Pipelines:** `DefaultRagPipeline`, lógica de middleware.
    *   **Retrieval:** `VectorRetriever` (usando `IVectorStore`), `KeywordRetriever`, `HybridRetriever` (con lógica RRF).
    *   **Ingestion:** `EmbeddingSimilarityChunker`, implementaciones de `IMetadataEnricher` basadas en `IChatClient`.
    *   **Transformación:** `QueryRewriter`, `HyDETransformer` (basados en MEAI).
    *   **Observabilidad:** Creación de los `Activity` spans para trazabilidad.