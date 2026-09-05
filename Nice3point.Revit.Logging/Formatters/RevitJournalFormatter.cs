using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nice3point.Revit.Logging.Formatters;

/// <summary>
///     Allows custom journal record formatting.
/// </summary>
/// <remarks>
///     Revit prepends the comment marker, the nesting depth and the optional time stamp, and opens every line after the first with an apostrophe.
///     A formatter writes only the text of the record.
/// </remarks>
/// <example>
///     <code lang="csharp">
/// public sealed class SingleLineFormatter : RevitJournalFormatter
/// {
///     public override void Write&lt;TState&gt;(in LogEntry&lt;TState&gt; logEntry, IExternalScopeProvider? scopeProvider, StringBuilder record)
///     {
///         record.Append(logEntry.LogLevel)
///             .Append(' ')
///             .Append(logEntry.Formatter(logEntry.State, logEntry.Exception));
///     }
/// }
/// 
/// builder.Logging.AddRevitJournal(application);
/// builder.Logging.AddRevitJournalFormatter&lt;SingleLineFormatter&gt;();
/// </code>
/// </example>
[PublicAPI]
public abstract class RevitJournalFormatter
{
    /// <summary>
    ///     Writes the text of the journal comment for the specified record.
    /// </summary>
    /// <param name="logEntry">The record to render.</param>
    /// <param name="scopeProvider">The scopes the record was created in, or <see langword="null" /> when the provider carries none.</param>
    /// <param name="record">The buffer the text is written to. It is empty on entry and reused between records.</param>
    /// <typeparam name="TState">The type of the record state.</typeparam>
    /// <remarks>Leaving the buffer empty discards the record.</remarks>
    public abstract void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, StringBuilder record);
}
