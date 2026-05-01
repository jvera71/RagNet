# 6. Diseño del Módulo de Abstracciones (`RagNet.Abstractions`)

## Parte 2 — Interfaces Core: Ingestión y Recuperación

> **Documento:** `docs/06-02-abstractions-interfaces-core.md`  
> **Versión:** 1.0  
> **Última actualización:** 2026-05-01

---

## 6.3. Interfaces Core — Catálogo Completo

Las interfaces de `RagNet.Abstractions` definen los puntos de extensión del sistema. Cada interfaz representa una **responsabilidad atómica** que puede ser implementada, reemplazada o decorada de forma independiente.

### Convenciones de diseño comunes

Todas las interfaces siguen estas convenciones:

1. **Asincronía total:** Todos los métodos retornan `Task<T>` o `IAsyncEnumerable<T>`. No existen variantes síncronas.
2. **CancellationToken:** Todos los métodos aceptan un `CancellationToken` como último parámetro con valor por defecto `default`.
3. **Inmutabilidad de entrada:** Los parámetros de entrada nunca son mutados. Los resultados son colecciones nuevas.
4. **Nomenclatura:** Sufijo `Async` en todos los métodos asíncronos.

---

### 6.3.1. `IDocumentParser` — Parsing de Documentos

**Dominio:** Ingestión  
**Implementaciones previstas:** `MarkdownDocumentParser`, `WordDocumentParser`, `ExcelDocumentParser`, `PdfDocumentParser`  
**Proyectos que implementan:** `Parsers.Markdown`, `Parsers.Office`, `Parsers.Pdf`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Transforma un documento binario en una estructura jerárquica de nodos,
/// preservando la estructura original (títulos, párrafos, listas, tablas).
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Formatos de archivo soportados por este parser (e.g., ".pdf", ".docx").
    /// </summary>
    IReadOnlySet<string> SupportedExtensions { get; }

    /// <summary>
    /// Parsea un documento desde un stream y retorna su representación jerárquica.
    /// </summary>
    /// <param name="documentStream">Stream del archivo fuente.</param>
    /// <param name="fileName">Nombre del archivo (para determinar formato y metadatos).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Nodo raíz del árbol jerárquico del documento.</returns>
    Task<DocumentNode> ParseAsync(
        Stream documentStream,
        string fileName,
        CancellationToken ct = default);
}
```

**Decisiones de diseño:**

| Decisión | Justificación |
|----------|--------------|
| `Stream` en vez de `byte[]` | Soporta archivos grandes sin cargar todo en memoria. |
| `SupportedExtensions` como propiedad | Permite al pipeline resolver automáticamente qué parser usar para cada archivo (Registry Pattern). |
| Retorna `DocumentNode` (árbol) | El chunker semántico necesita la jerarquía para tomar decisiones de particionado inteligentes. |

**Ejemplo de resolución automática de parser:**

```csharp
// El pipeline puede resolver el parser apropiado así:
var parser = parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension))
    ?? throw new UnsupportedFormatException(extension);
```

---

### 6.3.2. `ISemanticChunker` — Particionado Semántico

**Dominio:** Ingestión  
**Implementaciones previstas:** `NLPBoundaryChunker`, `MarkdownStructureChunker`, `EmbeddingSimilarityChunker`  
**Proyecto que implementa:** `RagNet.Core`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Analiza la estructura jerárquica de un documento parseado y lo divide
/// en fragmentos (chunks) semánticamente coherentes, listos para ser
/// embebidos y almacenados.
/// </summary>
public interface ISemanticChunker
{
    /// <summary>
    /// Divide un documento parseado en fragmentos semánticos.
    /// </summary>
    /// <param name="rootNode">Nodo raíz del documento parseado.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Colección de documentos RAG (chunks) sin vector asignado.</returns>
    Task<IEnumerable<RagDocument>> ChunkAsync(
        DocumentNode rootNode,
        CancellationToken ct = default);
}
```

**Comparativa de implementaciones previstas:**

| Implementación | Estrategia | Cuándo usarla |
|---------------|-----------|---------------|
| `NLPBoundaryChunker` | Detecta límites lingüísticos (fin de oración, cambio de tema) | Documentos con texto libre sin estructura clara |
| `MarkdownStructureChunker` | Usa la jerarquía de headings (H1, H2, H3) como límites naturales | Documentos Markdown o con estructura clara de secciones |
| `EmbeddingSimilarityChunker` | Agrupa oraciones consecutivas hasta que el embedding de la siguiente difiere significativamente (umbral configurable) | Máxima coherencia semántica; requiere `IEmbeddingGenerator` |

**Contrato implícito:** Los `RagDocument` retornados tienen `Content` definido y `Metadata` parcial (fuente, posición), pero `Vector` está vacío (`ReadOnlyMemory<float>.Empty`). El embedding se genera en una etapa posterior del pipeline.

---

### 6.3.3. `IMetadataEnricher` — Enriquecimiento de Metadatos

**Dominio:** Ingestión  
**Implementación principal:** Basada en `IChatClient` (MEAI) en `RagNet.Core`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Enriquece los metadatos de un chunk utilizando análisis automático
/// (típicamente vía LLM). Extrae entidades, palabras clave, resúmenes, etc.
/// </summary>
public interface IMetadataEnricher
{
    /// <summary>
    /// Enriquece un lote de documentos con metadatos adicionales extraídos automáticamente.
    /// </summary>
    /// <param name="documents">Documentos a enriquecer.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Documentos con metadatos enriquecidos en su diccionario Metadata.</returns>
    Task<IEnumerable<RagDocument>> EnrichAsync(
        IEnumerable<RagDocument> documents,
        CancellationToken ct = default);
}
```

**Metadatos típicos extraídos:**

| Clave en `Metadata` | Tipo | Descripción |
|---------------------|------|-------------|
| `"entities"` | `string[]` | Entidades nombradas (personas, organizaciones, lugares) |
| `"keywords"` | `string[]` | Palabras clave representativas del chunk |
| `"summary"` | `string` | Resumen breve (1-2 oraciones) del contenido |
| `"language"` | `string` | Idioma detectado del contenido |
| `"topic"` | `string` | Tema o categoría principal |

**¿Por qué opera en lotes (`IEnumerable`)?** Eficiencia. Permite a la implementación agrupar llamadas al LLM (batching), reduciendo latencia y costes. Una implementación puede enviar múltiples chunks en un solo prompt.

---

### 6.3.4. `IQueryTransformer` — Transformación de Consultas

**Dominio:** Recuperación  
**Implementaciones previstas:** `QueryRewriter`, `HyDETransformer`, `StepBackTransformer`  
**Proyecto que implementa:** `RagNet.Core`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Transforma la consulta original del usuario en una o más consultas
/// optimizadas para mejorar el recall en la recuperación de documentos.
/// </summary>
public interface IQueryTransformer
{
    /// <summary>
    /// Transforma una consulta original en consultas optimizadas.
    /// </summary>
    /// <param name="originalQuery">Consulta original del usuario.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Una o más consultas transformadas.</returns>
    Task<IEnumerable<string>> TransformAsync(
        string originalQuery,
        CancellationToken ct = default);
}
```

**¿Por qué retorna `IEnumerable<string>` (múltiples queries)?**

Diferentes técnicas de transformación pueden generar más de una consulta:

| Implementación | Queries generadas | Ejemplo |
|---------------|-------------------|---------|
| `QueryRewriter` | 1 (reescritura) | `"¿Qué es RAG?"` → `"Definición y funcionamiento de Retrieval Augmented Generation"` |
| `HyDETransformer` | 1 (documento hipotético) | `"¿Qué es RAG?"` → `"RAG es una técnica que combina recuperación de documentos con generación..."` |
| `StepBackTransformer` | 2+ (query original + abstraída) | `"¿Cuánta RAM necesita Qdrant?"` → `["¿Cuánta RAM necesita Qdrant?", "Requisitos de sistema de bases de datos vectoriales"]` |

El `IRetriever` aguas abajo ejecuta la búsqueda para **cada** query transformada y fusiona resultados.

---

### 6.3.5. `IRetriever` — Recuperación de Documentos

**Dominio:** Recuperación  
**Implementaciones previstas:** `VectorRetriever`, `KeywordRetriever`, `HybridRetriever`  
**Proyecto que implementa:** `RagNet.Core`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Recupera documentos relevantes desde el almacenamiento vectorial
/// o de texto completo, dado una consulta y un número máximo de resultados.
/// </summary>
public interface IRetriever
{
    /// <summary>
    /// Busca y retorna los documentos más relevantes para la consulta dada.
    /// </summary>
    /// <param name="query">Consulta de búsqueda (texto o embedding según implementación).</param>
    /// <param name="topK">Número máximo de documentos a retornar.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Documentos ordenados por relevancia descendente.</returns>
    Task<IEnumerable<RagDocument>> RetrieveAsync(
        string query,
        int topK,
        CancellationToken ct = default);
}
```

**Matriz de implementaciones:**

| Implementación | Motor de búsqueda | Dependencia | Caso de uso |
|---------------|-------------------|-------------|-------------|
| `VectorRetriever` | Búsqueda por similitud vectorial (ANN) | MEVD (`IVectorStore`) | Búsqueda semántica pura |
| `KeywordRetriever` | Full-Text Search (BM25, TF-IDF) | Proveedor FTS externo | Coincidencia exacta de términos |
| `HybridRetriever` | Combina vector + keyword con RRF | Ambos | Máximo recall combinando ambas estrategias |

**Contrato de ordenación:** Los documentos retornados **deben estar ordenados por relevancia descendente** (el más relevante primero). El `RelevanceScore` puede almacenarse en `Metadata["_score"]`.

---

### 6.3.6. `IDocumentReranker` — Reordenamiento de Resultados

**Dominio:** Recuperación  
**Implementaciones previstas:** `CrossEncoderReranker`, `LLMReranker`  
**Proyecto que implementa:** `RagNet.Core`

```csharp
namespace RagNet.Abstractions;

/// <summary>
/// Reordena una lista de documentos candidatos según su relevancia real
/// para la consulta, utilizando modelos más precisos (pero más costosos)
/// que la búsqueda vectorial inicial.
/// </summary>
public interface IDocumentReranker
{
    /// <summary>
    /// Reordena los documentos por relevancia y retorna los Top-K mejores.
    /// </summary>
    /// <param name="query">Consulta original del usuario.</param>
    /// <param name="documents">Documentos candidatos a reordenar.</param>
    /// <param name="topK">Número máximo de documentos a retornar tras el reordenamiento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Top-K documentos reordenados por relevancia descendente.</returns>
    Task<IEnumerable<RagDocument>> RerankAsync(
        string query,
        IEnumerable<RagDocument> documents,
        int topK,
        CancellationToken ct = default);
}
```

**Patrón de uso: ampliar y recortar (Retrieve-then-Rerank)**

```
IRetriever (Top-20 candidatos, búsqueda rápida)
       │
       ▼
IDocumentReranker (Top-5 relevantes, análisis profundo)
       │
       ▼
IRagGenerator (genera respuesta con 5 documentos de contexto)
```

**Comparativa de implementaciones:**

| Implementación | Modelo | Precisión | Coste | Latencia |
|---------------|--------|-----------|-------|----------|
| `CrossEncoderReranker` | Modelos cross-encoder especializados (e.g., ms-marco-MiniLM) | Alta | Bajo (modelo local) | Baja |
| `LLMReranker` | LLM general vía `IChatClient` (MEAI) | Muy alta | Alto (tokens LLM) | Alta |

---

> [!NOTE]
> Continúa en [Parte 3 — Interfaces de Pipeline y Diagrama de Clases](./06-03-abstractions-pipeline-y-diagrama.md): interfaces `IRagPipeline`, `IRagGenerator`, y diagrama de clases completo del módulo.
