using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace RagNet.SemanticKernel.Plugins;

/// <summary>
/// Citation plugin - automatically formats references to source documents.
/// </summary>
public class CitationPlugin
{
    /// <summary>
    /// Formats a reference to a source document.
    /// </summary>
    /// <param name="documentIndex">The index of the document (e.g. 1, 2, 3).</param>
    /// <param name="sourceFile">The name of the source file.</param>
    /// <returns>A formatted citation string.</returns>
    [KernelFunction("format_citation")]
    [Description("Formats a reference to a source document")]
    public string FormatCitation(int documentIndex, string sourceFile)
    {
        return $"[{documentIndex}] ({sourceFile})";
    }
}
