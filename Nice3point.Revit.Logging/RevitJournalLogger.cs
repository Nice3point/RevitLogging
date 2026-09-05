using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nice3point.Revit.Logging.Formatters;
using Nice3point.Revit.Logging.Writers;

namespace Nice3point.Revit.Logging;

/// <summary>
///     Writes the records of a single category to the Revit journal, one comment per record.
/// </summary>
internal sealed class RevitJournalLogger(
    string category,
    IJournalWriter writer,
    RevitJournalFormatter recordFormatter,
    IOptionsMonitor<RevitJournalLoggerOptions> options)
    : ILogger
{
    private const int RetainedCapacity = 1024;

    /// <summary>
    ///     Buffer a record is rendered into, held per thread and reused between records.
    /// </summary>
    /// <remarks>A thread that rendered an oversized record trims the buffer back to <see cref="RetainedCapacity" />.</remarks>
    [ThreadStatic] private static StringBuilder? _record;

    /// <summary>
    ///     The scopes of the provider, assigned by the logging factory.
    /// </summary>
    public IExternalScopeProvider? ScopeProvider { get; set; }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return ScopeProvider?.Push(state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var record = _record ??= new StringBuilder();
        var entry = new LogEntry<TState>(logLevel, category, eventId, state, exception, formatter);
        recordFormatter.Write(entry, ScopeProvider, record);

        if (record.Length > 0)
        {
            WriteComment(record.ToString());
        }

        record.Clear();
        if (record.Capacity > RetainedCapacity)
        {
            record.Capacity = RetainedCapacity;
        }
    }

    private void WriteComment(string comment)
    {
        try
        {
            writer.WriteComment(Sanitize(comment), options.CurrentValue.IncludeTimestamp);
        }
        catch (Exception)
        {
            //A record the journal rejects must not take the add-in down with it
        }
    }

    /// <summary>
    ///     Replaces the characters Revit cannot carry through a journal comment.
    /// </summary>
    /// <remarks>A null character truncates the comment at the point it appears.</remarks>
    private static string Sanitize(string comment)
    {
        return comment.IndexOf('\0') < 0 ? comment : comment.Replace('\0', ' ');
    }
}
