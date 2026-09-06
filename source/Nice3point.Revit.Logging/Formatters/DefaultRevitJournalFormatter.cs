using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Nice3point.Revit.Logging.Formatters;

/// <summary>
///     Renders a record in the block shape Revit writes its own <c>API_SUCCESS</c> and <c>API_ERROR</c> comments in.
/// </summary>
/// <remarks>
///     The closing brace bounds the record, so a reader finds its end even when an exception carried the record across dozens of journal lines.
/// </remarks>
internal sealed class DefaultRevitJournalFormatter(IOptionsMonitor<RevitJournalLoggerOptions> options) : RevitJournalFormatter
{
    private const string ScopeSeparator = " => ";

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, StringBuilder record)
    {
        var currentOptions = options.CurrentValue;

        record.Append(currentOptions.ApplicationName)
            .Append('_')
            .Append(ResolveLevelName(logEntry.LogLevel))
            .Append(" { ");

        var headerStart = record.Length;

        if (currentOptions.IncludeCategory)
        {
            record.Append(logEntry.Category);
        }

        if (currentOptions.IncludeEventId)
        {
            record.Append('[').Append(logEntry.EventId.Id).Append(']');
        }

        if (currentOptions.IncludeScopes && scopeProvider is not null)
        {
            AppendScopes(record, scopeProvider, headerStart);
        }

        if (record.Length > headerStart)
        {
            record.Append(": ");
        }

        Append(record, logEntry.Formatter(logEntry.State, logEntry.Exception), currentOptions.SingleLine);

        if (logEntry.Exception is not null)
        {
            record.Append(currentOptions.SingleLine ? " " : Environment.NewLine);
            Append(record, logEntry.Exception.ToString(), currentOptions.SingleLine);
        }

        record.Append(" }");
    }

    private static void AppendScopes(StringBuilder record, IExternalScopeProvider scopeProvider, int headerStart)
    {
        scopeProvider.ForEachScope(static (scope, state) =>
        {
            if (state.Record.Length > state.HeaderStart)
            {
                state.Record.Append(ScopeSeparator);
            }

            state.Record.Append(scope);
        }, (Record: record, HeaderStart: headerStart));
    }

    private static void Append(StringBuilder record, string text, bool singleLine)
    {
        record.Append(singleLine ? text.ReplaceLineEndings(" ") : text);
    }

    private static string ResolveLevelName(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFORMATION",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            _ => "NONE"
        };
    }
}
