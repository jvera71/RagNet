*   **Propósito:** Actúa como el punto de entrada amigable para el desarrollador. Expone los métodos de extensión para la Inyección de Dependencias y los *Builders* de configuración fluida. En muchos casos, este paquete simplemente referencia a `RagNet.Core` y proporciona la API pública simplificada.
*   **Dependencias:** `RagNet.Core`, `Microsoft.Extensions.DependencyInjection`.
*   **Componentes del diseño:**
    *   `RagPipelineBuilder`, `IngestionPipelineBuilder`.
    *   Métodos de extensión: `builder.Services.AddAdvancedRag(...)`.
    *   Factories: `IRagPipelineFactory`.
