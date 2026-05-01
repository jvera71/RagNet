# RagNet

## 1. Visión General y Objetivos
**`RagNet`** es una biblioteca de clases .NET diseñada para facilitar la construcción de sistemas de Generación Aumentada por Recuperación (RAG) de nivel empresarial. 

Se basa en tres pilares tecnológicos nativos de Microsoft:
1.  **`Microsoft.Extensions.AI` (MEAI):** Abstracciones estándar para modelos de lenguaje (`IChatClient`) y generación de embeddings (`IEmbeddingGenerator`).
2.  **`Microsoft.Extensions.VectorData` (MEVD):** Abstracciones estándar para bases de datos vectoriales (`IVectorStore`).
3.  **`Semantic Kernel` (SK):** Orquestación, plantillas de prompts, invocación de herramientas y agentes.

### 1.1 Objetivos de Diseño
*   **Modularidad:** Los componentes (Chunkers, Retrievers, Transformers) deben ser intercambiables mediante Inyección de Dependencias (DI).
*   **Developer Experience (DX):** Uso de patrones `Builder` para configurar pipelines complejos de manera fluida (Fluent API).
*   **Observabilidad:** Integración nativa con `System.Diagnostics.Activity` (OpenTelemetry) para trazar cada paso del pipeline RAG.

---

## 2. Arquitectura de Alto Nivel

La biblioteca se divide en tres capas funcionales principales:

```mermaid
+-------------------------------------------------------------+
|                   API Pública / Builders                    |
|  (RagPipelineBuilder, IngestionPipelineBuilder, Extensions) |
+-------------------------------------------------------------+
|                       Motor Core RAG                        |
| +----------------+ +------------------+ +-----------------+ |
| |  Ingestion     | |  Retrieval       | |  Generation     | |
| | - Parsers      | | - Q. Transform   | | - SK Prompts    | |
| | - Semantic Chnk| | - Hybrid Search  | | - Citation Gen  | |
| | - Metadata Extr| | - Re-Ranking     | | - Streaming     | |
| +----------------+ +------------------+ +-----------------+ |
+-------------------------------------------------------------+
|               Abstracciones de Infraestructura              |
| [Semantic Kernel]  [Microsoft.Extensions.AI]  [M.E.VectorData]|
+-------------------------------------------------------------+
```

---

## 3. Diseño de Componentes Principales

### 3.1 Módulo de Ingestión Inteligente (Ingestion)
El objetivo es abandonar el particionado estático ("split by length") a favor de estrategias semánticas y estructurales.

*   **`IDocumentParser`**: Transforma formatos binarios (PDF, Word, HTML) en una estructura jerárquica intermedia (`DocumentNode`), preservando títulos, listas y párrafos.
*   **`ISemanticChunker`**: Analiza los nodos del documento y agrupa oraciones o párrafos basándose en la similitud semántica o límites estructurales.
    *   *Implementaciones:* `NLPBoundaryChunker`, `MarkdownStructureChunker`, `EmbeddingSimilarityChunker` (agrupa frases hasta que el embedding de la siguiente frase difiere demasiado).
*   **`IMetadataEnricher`**: Utiliza `IChatClient` (MEAI) para extraer metadatos automáticamente de cada chunk (entidades nombradas, palabras clave, resúmenes breves) antes de guardarlos. Esto es clave para la búsqueda híbrida.
*   **Pipeline de Ingestión:** Orquesta el flujo: *Raw File -> Parse -> Semantic Chunk -> Enrich -> Embed (`IEmbeddingGenerator`) -> Save (`IVectorStore`)*.

### 3.2 Módulo de Recuperación Avanzada (Retrieval)
Este módulo se encarga de optimizar la consulta del usuario y obtener los fragmentos más relevantes, mitigando el problema del "Lost in the Middle".

*   **`IQueryTransformer`**: Modifica la consulta original para mejorar el *recall*.
    *   *Implementaciones:*
        *   `QueryRewriter`: Usa MEAI para reescribir consultas ambiguas.
        *   `HyDETransformer`: Genera una respuesta hipotética usando MEAI y vectoriza esa respuesta para buscar.
        *   `StepBackTransformer`: Extrae principios o conceptos más amplios de la consulta.
*   **`IRetriever`**: Abstracción sobre las búsquedas reales.
    *   *Implementaciones:* `VectorRetriever` (usa MEVD), `KeywordRetriever` (Full-Text Search clásico), `HybridRetriever` (combina ambos usando Fusión de Rangos Recíprocos - RRF).
*   **`IDocumentReranker`**: Toma una lista amplia de resultados (ej. Top 20) y los reordena quedándose con los mejores (ej. Top 5).
    *   *Implementaciones:* `CrossEncoderReranker` (modelos especializados), `LLMReranker` (usa MEAI para puntuar relevancia).

### 3.3 Módulo de Generación (Generation)
Conecta los resultados recuperados con Semantic Kernel para la síntesis de respuestas.

*   **`IRagGenerator`**: Utiliza Semantic Kernel para fusionar el contexto recuperado con el prompt del sistema.
*   *Características:* Soporte para streaming de respuestas, inyección automática de citas/referencias, y validación de alucinaciones (Self-RAG).

---

## 4. Contratos de Interfaz (Core Interfaces)

A continuación, un esbozo de las interfaces fundamentales que definirán el diseño de la biblioteca:

```csharp
namespace AdvancedRAG.Core;

// 1. Representación Unificada de Datos
public record RagDocument(
    string Id, 
    string Content, 
    ReadOnlyMemory<float> Vector, 
    Dictionary<string, object> Metadata);

// 2. Transformación de Consultas
public interface IQueryTransformer
{
    Task<IEnumerable<string>> TransformAsync(string originalQuery, CancellationToken ct = default);
}

// 3. Recuperación (Retrieval)
public interface IRetriever
{
    Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK, CancellationToken ct = default);
}

// 4. Re-Ranking
public interface IDocumentReranker
{
    Task<IEnumerable<RagDocument>> RerankAsync(string query, IEnumerable<RagDocument> documents, int topK, CancellationToken ct = default);
}

// 5. Pipeline RAG Principal
public interface IRagPipeline
{
    Task<RagResponse> ExecuteAsync(string query, CancellationToken ct = default);
    IAsyncEnumerable<StreamingRagResponse> ExecuteStreamingAsync(string query, CancellationToken ct = default);
}
```

---

## 5. Patrones de Diseño Recomendados

1.  **Pipeline Pattern (Middleware):** Utilizado para el `IRagPipeline`. Permite a los desarrolladores insertar pasos personalizados. Funciona de manera similar a los middlewares de ASP.NET Core.
2.  **Builder Pattern:** Para la configuración fluida del sistema.
    *   `RagPipelineBuilder`
    *   `IngestionPipelineBuilder`
3.  **Strategy Pattern:** Para permitir el intercambio fácil de algoritmos de particionado (Chunkers), transformación (Transformers) y reordenamiento (Rerankers).
4.  **Decorator Pattern:** Para añadir capacidades transversales como Caché (Semantic Caching), Logging de prompts, o resiliencia (Polly) sin modificar la lógica core.

---

## 6. Diseño de la API Pública (Developer Experience - DX)

El éxito de una biblioteca .NET radica en lo fácil que es configurarla en `Program.cs`. Así es como debería verse el uso de la biblioteca:

### 6.1 Registro en DI (Dependency Injection)

```csharp
// Configuración de infraestructura (Microsoft.Extensions.AI / VectorData)
builder.Services.AddChatClient(new OpenAIClient(...));
builder.Services.AddEmbeddingGenerator(new OpenAIEmbeddingGenerator(...));
builder.Services.AddVectorStore(new QdrantVectorStore(...));

// Configuración de AdvancedRAG
builder.Services.AddAdvancedRag(rag => 
{
    // Configurar Ingestión
    rag.AddIngestion(ingest => ingest
        .UseSemanticChunker(new SemanticChunkerOptions { OverlapSimilarityThreshold = 0.85 })
        .UseLLMMetadataEnrichment(extractEntities: true)
    );

    // Configurar Pipeline RAG Avanzado
    rag.AddPipeline("MyAdvancedPipeline", pipeline => pipeline
        .UseQueryTransformation<HyDETransformer>()
        .UseHybridRetrieval(alpha: 0.5) // 0.5 = 50% Vector, 50% Keyword
        .UseReranking<LLMReranker>(topK: 5)
        .UseSemanticKernelGenerator(promptTemplate: "Responde basándote solo en esto: {{context}}")
    );
});
```

### 6.2 Uso en la Aplicación

```csharp
public class ChatService
{
    private readonly IRagPipeline _ragPipeline;

    // Se inyecta usando una Factory o directamente si hay uno solo por defecto
    public ChatService(IRagPipelineFactory pipelineFactory)
    {
        _ragPipeline = pipelineFactory.Create("MyAdvancedPipeline");
    }

    public async Task<string> AskQuestionAsync(string userQuery)
    {
        RagResponse response = await _ragPipeline.ExecuteAsync(userQuery);
        
        // La respuesta incluye las citas/fuentes automáticas
        foreach(var citation in response.Citations)
        {
            Console.WriteLine($"Fuente: {citation.DocumentId} - Confianza: {citation.RelevanceScore}");
        }

        return response.Answer;
    }
}
```

---

## 7. Consideraciones Técnicas y de Implementación

1.  **Compatibilidad de Vectores:** Dado que `Microsoft.Extensions.VectorData` utiliza genéricos (`IVectorStoreRecordCollection<TKey, TRecord>`), la biblioteca debe proporcionar clases de registros estándar (ej. `DefaultRagVectorRecord`) anotadas con los atributos de MEVD (`[VectorStoreRecordVector]`, `[VectorStoreRecordData]`), pero permitiendo a los usuarios mapear sus propias clases.
2.  **Streaming End-to-End:** El `IRagGenerator` debe exponer interfaces `IAsyncEnumerable` para poder enviar tokens a la UI mientras Semantic Kernel / MEAI generan la respuesta, sin esperar a que termine.
3.  **Manejo del Context Window:** Incorporar un tokenizador (ej. `Microsoft.ML.Tokenizers`) en la fase de generación. Si los documentos recuperados exceden la ventana de contexto del LLM, el pipeline debe truncarlos inteligentemente o resumirlos dinámicamente antes de enviarlos al Semantic Kernel.
4.  **OpenTelemetry:** Todas las interfaces (Ingestion, Retrieval, Generation) deben crear `Activity` spans (ej. `AdvancedRag.Retrieval`, `AdvancedRag.Reranking`). Esto permite visualizar el RAG completo en herramientas como Aspire Dashboard o Application Insights.