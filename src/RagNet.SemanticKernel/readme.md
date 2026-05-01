*   **Propósito:** Proporciona la implementación específica del módulo de Generación que depende de Semantic Kernel. Se separa del Core porque SK es una dependencia robusta y en constante evolución; algunos usuarios podrían preferir usar MEAI directamente.
*   **Dependencias:** `RagNet.Core`, `Microsoft.SemanticKernel`.
*   **Componentes del diseño:**
    *   `SemanticKernelRagGenerator` (implementación de `IRagGenerator`).
    *   Funciones y plugins de SK específicos para RAG.
    *   Lógica de inyección de citas y prompts avanzados usando el motor de plantillas de SK.