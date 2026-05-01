*   **Propósito:** Contiene exclusivamente las interfaces core, modelos de dominio, y registros (`records`). Es el núcleo de la arquitectura y no debe tener lógica de negocio pesada.
*   **Dependencias:** Ninguna (o dependencias muy ligeras de Microsoft, como `Microsoft.Bcl.AsyncInterfaces`).
*   **Componentes del diseño:** 
    *   `RagDocument`, `RagResponse`, `StreamingRagResponse`.
    *   `IRetriever`, `IQueryTransformer`, `IDocumentReranker`, `IRagPipeline`, `ISemanticChunker`, `IDocumentParser`.