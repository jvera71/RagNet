# RagNet — Documentación de Arquitectura y Diseño Técnico

> **Versión del documento:** 1.0-draft  
> **Fecha:** 2026-05-01  
> **Estado:** Índice — Pendiente de redacción de secciones  
> **Plataforma objetivo:** .NET 8.0

---

## Índice General

### 1. Introducción y Visión General
`📄 docs/01-introduccion.md`

1.1. Propósito del documento  
1.2. Alcance de la biblioteca RagNet  
1.3. Audiencia objetivo  
1.4. Glosario de términos y acrónimos  
1.5. Convenciones de notación utilizadas  

---

### 2. Contexto y Objetivos del Sistema
`📄 docs/02-contexto-objetivos.md`

2.1. Problema que resuelve RagNet  
2.2. Posicionamiento frente a alternativas existentes  
2.3. Objetivos de diseño  
&emsp;&emsp;2.3.1. Modularidad e intercambiabilidad (DI)  
&emsp;&emsp;2.3.2. Developer Experience (DX) — Fluent API y Builders  
&emsp;&emsp;2.3.3. Observabilidad nativa (OpenTelemetry)  
2.4. Restricciones y supuestos  
2.5. Principios arquitectónicos rectores  

---

### 3. Pilares Tecnológicos
`📄 docs/03-pilares-tecnologicos.md`

3.1. Microsoft.Extensions.AI (MEAI)  
&emsp;&emsp;3.1.1. `IChatClient` — Abstracción de modelos de lenguaje  
&emsp;&emsp;3.1.2. `IEmbeddingGenerator` — Generación de embeddings  
&emsp;&emsp;3.1.3. Modelo de extensibilidad y middleware de MEAI  
3.2. Microsoft.Extensions.VectorData (MEVD)  
&emsp;&emsp;3.2.1. `IVectorStore` y `IVectorStoreRecordCollection<TKey, TRecord>`  
&emsp;&emsp;3.2.2. Atributos de mapeo: `[VectorStoreRecordVector]`, `[VectorStoreRecordData]`  
&emsp;&emsp;3.2.3. Proveedores soportados (Qdrant, Azure AI Search, etc.)  
3.3. Semantic Kernel (SK)  
&emsp;&emsp;3.3.1. Orquestación y Kernel  
&emsp;&emsp;3.3.2. Motor de plantillas de prompts  
&emsp;&emsp;3.3.3. Invocación de herramientas (Tool Use) y Agentes  
3.4. Mapa de versiones y compatibilidad  

---

### 4. Arquitectura de Alto Nivel
`📄 docs/04-arquitectura-alto-nivel.md`

4.1. Vista de capas (Layered Architecture)  
&emsp;&emsp;4.1.1. Capa de API Pública / Builders  
&emsp;&emsp;4.1.2. Capa del Motor Core RAG  
&emsp;&emsp;4.1.3. Capa de Abstracciones de Infraestructura  
4.2. Diagrama de arquitectura general (C4 — Nivel Contexto)  
4.3. Diagrama de contenedores (C4 — Nivel Contenedor)  
4.4. Flujo de datos end-to-end  
&emsp;&emsp;4.4.1. Flujo de Ingestión  
&emsp;&emsp;4.4.2. Flujo de Consulta (Query → Response)  
4.5. Principio de separación de concerns entre capas  

---

### 5. Estructura de la Solución y Proyectos
`📄 docs/05-estructura-solucion.md`

5.1. Organización de la solución (`RagNet.slnx`)  
5.2. Mapa de proyectos y responsabilidades  
&emsp;&emsp;5.2.1. `RagNet.Abstractions` — Contratos y modelos de dominio  
&emsp;&emsp;5.2.2. `RagNet.Core` — Lógica principal y orquestación  
&emsp;&emsp;5.2.3. `RagNet` — API pública, Builders y DI  
&emsp;&emsp;5.2.4. `RagNet.SemanticKernel` — Integración con SK  
&emsp;&emsp;5.2.5. `RagNet.Parsers.Markdown` — Parser de Markdown  
&emsp;&emsp;5.2.6. `RagNet.Parsers.Office` — Parser de Word/Excel  
&emsp;&emsp;5.2.7. `RagNet.Parsers.Pdf` — Parser de PDF  
5.3. Grafo de dependencias entre proyectos  
5.4. Dependencias externas (NuGet)  
&emsp;&emsp;5.4.1. Microsoft.Extensions.AI (v10.5.0)  
&emsp;&emsp;5.4.2. Microsoft.Extensions.VectorData.Abstractions (v10.5.0)  
&emsp;&emsp;5.4.3. Microsoft.SemanticKernel (v1.75.0)  
&emsp;&emsp;5.4.4. Markdig (v1.1.3)  
&emsp;&emsp;5.4.5. DocumentFormat.OpenXml (v3.5.1)  
&emsp;&emsp;5.4.6. PdfPig (v0.1.14)  
5.5. Estrategia de versionado de paquetes  

---

### 6. Diseño del Módulo de Abstracciones (`RagNet.Abstractions`)
`📄 docs/06-modulo-abstractions.md`

6.1. Filosofía de diseño: contratos ligeros sin lógica de negocio  
6.2. Modelos de dominio  
&emsp;&emsp;6.2.1. `RagDocument` — Representación unificada de datos  
&emsp;&emsp;6.2.2. `DocumentNode` — Estructura jerárquica intermedia  
&emsp;&emsp;6.2.3. `RagResponse` — Respuesta del pipeline  
&emsp;&emsp;6.2.4. `StreamingRagResponse` — Respuesta en streaming  
6.3. Interfaces core — Catálogo completo  
&emsp;&emsp;6.3.1. `IDocumentParser` — Parsing de documentos  
&emsp;&emsp;6.3.2. `ISemanticChunker` — Particionado semántico  
&emsp;&emsp;6.3.3. `IMetadataEnricher` — Enriquecimiento de metadatos  
&emsp;&emsp;6.3.4. `IQueryTransformer` — Transformación de consultas  
&emsp;&emsp;6.3.5. `IRetriever` — Recuperación de documentos  
&emsp;&emsp;6.3.6. `IDocumentReranker` — Reordenamiento de resultados  
&emsp;&emsp;6.3.7. `IRagPipeline` — Pipeline RAG principal  
&emsp;&emsp;6.3.8. `IRagGenerator` — Generación de respuestas  
6.4. Diagrama de clases del módulo  

---

### 7. Diseño del Módulo de Ingestión Inteligente
`📄 docs/07-modulo-ingestion.md`

7.1. Visión general del pipeline de ingestión  
&emsp;&emsp;7.1.1. Flujo: Raw File → Parse → Semantic Chunk → Enrich → Embed → Save  
&emsp;&emsp;7.1.2. Diagrama de secuencia  
7.2. Parsing de documentos (`IDocumentParser`)  
&emsp;&emsp;7.2.1. Diseño de `DocumentNode` y estructura jerárquica  
&emsp;&emsp;7.2.2. Implementaciones por formato  
&emsp;&emsp;&emsp;&emsp;7.2.2.1. `MarkdownDocumentParser` (Markdig)  
&emsp;&emsp;&emsp;&emsp;7.2.2.2. `WordDocumentParser` (OpenXml)  
&emsp;&emsp;&emsp;&emsp;7.2.2.3. `ExcelDocumentParser` (OpenXml)  
&emsp;&emsp;&emsp;&emsp;7.2.2.4. `PdfDocumentParser` (PdfPig)  
&emsp;&emsp;7.2.3. Extensión: cómo añadir nuevos parsers  
7.3. Particionado semántico (`ISemanticChunker`)  
&emsp;&emsp;7.3.1. Problema del particionado estático vs. semántico  
&emsp;&emsp;7.3.2. `NLPBoundaryChunker` — Límites lingüísticos  
&emsp;&emsp;7.3.3. `MarkdownStructureChunker` — Límites estructurales  
&emsp;&emsp;7.3.4. `EmbeddingSimilarityChunker` — Similitud por embedding  
&emsp;&emsp;7.3.5. Configuración: umbrales, solapamiento, tamaño máximo  
7.4. Enriquecimiento de metadatos (`IMetadataEnricher`)  
&emsp;&emsp;7.4.1. Extracción automática vía LLM (entidades, palabras clave, resúmenes)  
&emsp;&emsp;7.4.2. Impacto en la búsqueda híbrida  
&emsp;&emsp;7.4.3. Diseño de prompts para enriquecimiento  
7.5. Generación de embeddings (`IEmbeddingGenerator`)  
&emsp;&emsp;7.5.1. Integración con MEAI  
&emsp;&emsp;7.5.2. Procesamiento por lotes (batching)  
7.6. Almacenamiento vectorial (`IVectorStore`)  
&emsp;&emsp;7.6.1. `DefaultRagVectorRecord` y mapeo de registros  
&emsp;&emsp;7.6.2. Esquemas de colección y configuración de índices  
7.7. Orquestación: `IngestionPipelineBuilder`  

---

### 8. Diseño del Módulo de Recuperación Avanzada
`📄 docs/08-modulo-retrieval.md`

8.1. Visión general y problema "Lost in the Middle"  
8.2. Transformación de consultas (`IQueryTransformer`)  
&emsp;&emsp;8.2.1. `QueryRewriter` — Reescritura de consultas ambiguas  
&emsp;&emsp;8.2.2. `HyDETransformer` — Hypothetical Document Embeddings  
&emsp;&emsp;8.2.3. `StepBackTransformer` — Abstracción de consultas  
&emsp;&emsp;8.2.4. Composición de transformadores  
8.3. Recuperación de documentos (`IRetriever`)  
&emsp;&emsp;8.3.1. `VectorRetriever` — Búsqueda vectorial (MEVD)  
&emsp;&emsp;8.3.2. `KeywordRetriever` — Full-Text Search  
&emsp;&emsp;8.3.3. `HybridRetriever` — Fusión híbrida  
&emsp;&emsp;&emsp;&emsp;8.3.3.1. Algoritmo de Reciprocal Rank Fusion (RRF)  
&emsp;&emsp;&emsp;&emsp;8.3.3.2. Parámetro alpha: balance vector/keyword  
8.4. Reordenamiento de resultados (`IDocumentReranker`)  
&emsp;&emsp;8.4.1. `CrossEncoderReranker` — Modelos especializados  
&emsp;&emsp;8.4.2. `LLMReranker` — Puntuación por LLM  
&emsp;&emsp;8.4.3. Estrategia Top-K: ampliación y recorte  
8.5. Diagrama de secuencia del pipeline de recuperación  

---

### 9. Diseño del Módulo de Generación
`📄 docs/09-modulo-generacion.md`

9.1. Rol de Semantic Kernel en la generación  
9.2. `IRagGenerator` y `SemanticKernelRagGenerator`  
9.3. Gestión de plantillas de prompts  
&emsp;&emsp;9.3.1. Inyección de contexto (`{{context}}`)  
&emsp;&emsp;9.3.2. Motor de plantillas de SK  
9.4. Streaming end-to-end (`IAsyncEnumerable`)  
&emsp;&emsp;9.4.1. Propagación de tokens a la UI  
&emsp;&emsp;9.4.2. Integración con `StreamingRagResponse`  
9.5. Inyección automática de citas y referencias  
&emsp;&emsp;9.5.1. Modelo `Citation` y `RelevanceScore`  
&emsp;&emsp;9.5.2. Trazabilidad de fuentes en la respuesta  
9.6. Validación de alucinaciones (Self-RAG)  
9.7. Manejo del Context Window  
&emsp;&emsp;9.7.1. Tokenización (`Microsoft.ML.Tokenizers`)  
&emsp;&emsp;9.7.2. Truncamiento inteligente  
&emsp;&emsp;9.7.3. Resumen dinámico de contexto  
9.8. Plugins y funciones de SK específicos para RAG  

---

### 10. Patrones de Diseño y API Pública
`📄 docs/10-patrones-api-publica.md`

10.1. Patrones de diseño aplicados  
&emsp;&emsp;10.1.1. Pipeline Pattern (Middleware)  
&emsp;&emsp;&emsp;&emsp;— Analogía con middlewares de ASP.NET Core  
&emsp;&emsp;&emsp;&emsp;— Pasos personalizados en `IRagPipeline`  
&emsp;&emsp;10.1.2. Builder Pattern  
&emsp;&emsp;&emsp;&emsp;— `RagPipelineBuilder`  
&emsp;&emsp;&emsp;&emsp;— `IngestionPipelineBuilder`  
&emsp;&emsp;10.1.3. Strategy Pattern  
&emsp;&emsp;&emsp;&emsp;— Intercambio de Chunkers, Transformers, Rerankers  
&emsp;&emsp;10.1.4. Decorator Pattern  
&emsp;&emsp;&emsp;&emsp;— Semantic Caching  
&emsp;&emsp;&emsp;&emsp;— Logging de prompts  
&emsp;&emsp;&emsp;&emsp;— Resiliencia (Polly)  
&emsp;&emsp;10.1.5. Factory Pattern  
&emsp;&emsp;&emsp;&emsp;— `IRagPipelineFactory`  
10.2. API Pública — Developer Experience (DX)  
&emsp;&emsp;10.2.1. Registro en Inyección de Dependencias (`Program.cs`)  
&emsp;&emsp;&emsp;&emsp;— `builder.Services.AddAdvancedRag(...)`  
&emsp;&emsp;&emsp;&emsp;— Configuración de Ingestión (`AddIngestion`)  
&emsp;&emsp;&emsp;&emsp;— Configuración de Pipelines nombrados (`AddPipeline`)  
&emsp;&emsp;10.2.2. Uso en la aplicación consumidora  
&emsp;&emsp;&emsp;&emsp;— Inyección de `IRagPipelineFactory`  
&emsp;&emsp;&emsp;&emsp;— Ejecución síncrona y en streaming  
&emsp;&emsp;10.2.3. Opciones de configuración (`Options Pattern`)  
&emsp;&emsp;&emsp;&emsp;— `SemanticChunkerOptions`  
&emsp;&emsp;&emsp;&emsp;— Opciones de pipeline  
10.3. Diagrama de interacción: configuración → ejecución  

---

### 11. Observabilidad y Trazabilidad
`📄 docs/11-observabilidad.md`

11.1. Estrategia de instrumentación  
11.2. Integración con `System.Diagnostics.Activity`  
&emsp;&emsp;11.2.1. Activity Sources por módulo  
&emsp;&emsp;11.2.2. Spans definidos  
&emsp;&emsp;&emsp;&emsp;— `RagNet.Ingestion.*`  
&emsp;&emsp;&emsp;&emsp;— `RagNet.Retrieval.*`  
&emsp;&emsp;&emsp;&emsp;— `RagNet.Reranking`  
&emsp;&emsp;&emsp;&emsp;— `RagNet.Generation`  
11.3. Métricas y tags personalizados  
11.4. Exportación a backends de observabilidad  
&emsp;&emsp;11.4.1. .NET Aspire Dashboard  
&emsp;&emsp;11.4.2. Azure Application Insights  
&emsp;&emsp;11.4.3. Jaeger / Zipkin  
11.5. Logging estructurado  
11.6. Health checks  

---

### 12. Consideraciones Transversales
`📄 docs/12-consideraciones-transversales.md`

12.1. Compatibilidad de vectores y registros  
&emsp;&emsp;12.1.1. `DefaultRagVectorRecord` con atributos MEVD  
&emsp;&emsp;12.1.2. Mapeo de clases de usuario  
12.2. Gestión de errores y resiliencia  
&emsp;&emsp;12.2.1. Políticas de retry con Polly  
&emsp;&emsp;12.2.2. Circuit breaker para llamadas a LLM  
&emsp;&emsp;12.2.3. Fallback strategies  
12.3. Rendimiento y escalabilidad  
&emsp;&emsp;12.3.1. Procesamiento asíncrono end-to-end  
&emsp;&emsp;12.3.2. Batching en ingestión y embedding  
&emsp;&emsp;12.3.3. Semantic Caching  
12.4. Seguridad  
&emsp;&emsp;12.4.1. Sanitización de entradas al LLM  
&emsp;&emsp;12.4.2. Gestión de secretos y credenciales  
12.5. Testing  
&emsp;&emsp;12.5.1. Unit testing con mocks de interfaces  
&emsp;&emsp;12.5.2. Integration testing del pipeline  
&emsp;&emsp;12.5.3. Benchmarking de calidad RAG  
12.6. Extensibilidad  
&emsp;&emsp;12.6.1. Cómo añadir un nuevo `IDocumentParser`  
&emsp;&emsp;12.6.2. Cómo añadir un nuevo `IRetriever`  
&emsp;&emsp;12.6.3. Cómo añadir un nuevo `IQueryTransformer`  
12.7. Guía de migración y adopción incremental  

---

### Apéndices
`📄 docs/apendices.md`

A. Catálogo completo de interfaces y signaturas  
B. Diagrama de dependencias de paquetes NuGet  
C. Matriz de decisión: Semantic Kernel vs. MEAI directo  
D. Ejemplo de integración completa (`Program.cs` comentado)  
E. Referencias y bibliografía  
&emsp;&emsp;— Microsoft.Extensions.AI (documentación oficial)  
&emsp;&emsp;— Microsoft.Extensions.VectorData (documentación oficial)  
&emsp;&emsp;— Semantic Kernel (documentación oficial)  
&emsp;&emsp;— Papers: RAG, HyDE, Reciprocal Rank Fusion, Self-RAG  

---

> [!NOTE]
> Cada sección será desarrollada en un documento individual dentro de la carpeta `docs/`. La referencia `📄` indica el archivo destino previsto para el contenido completo de cada sección.
