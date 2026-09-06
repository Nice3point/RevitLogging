using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nice3point.Revit.Logging.Formatters;
using Nice3point.Revit.Logging.Writers;

namespace Nice3point.Revit.Logging;

/// <summary>
///     Provides <see cref="ILogger" /> instances that write to the journal of the running Revit session.
/// </summary>
/// <remarks>
///     A record is written on the thread that logged it, both inside and outside the Revit API context.
///     The provider marshals nothing onto the Revit thread.
/// </remarks>
[PublicAPI]
[ProviderAlias("RevitJournal")]
public sealed class RevitJournalLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, RevitJournalLogger> _loggers = new(StringComparer.Ordinal);
    private readonly RevitJournalFormatter _formatter;
    private readonly IOptionsMonitor<RevitJournalLoggerOptions> _options;
    private readonly IJournalWriter _writer;

    private IExternalScopeProvider? _scopeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RevitJournalLoggerProvider" /> class.
    /// </summary>
    /// <param name="writer">The journal every record is written to.</param>
    /// <param name="formatter">The formatter every record is rendered with.</param>
    /// <param name="options">The options the provider reads on every record.</param>
    public RevitJournalLoggerProvider(IJournalWriter writer, RevitJournalFormatter formatter, IOptionsMonitor<RevitJournalLoggerOptions> options)
    {
        _writer = writer;
        _formatter = formatter;
        _options = options;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, static (category, provider) => new RevitJournalLogger(category, provider._writer, provider._formatter, provider._options)
        {
            ScopeProvider = provider._scopeProvider
        }, this);
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;

        foreach (var logger in _loggers.Values)
        {
            logger.ScopeProvider = scopeProvider;
        }
    }

    /// <inheritdoc />
    /// <remarks>This method does nothing. The provider does not own the Revit application the records are written to.</remarks>
    public void Dispose()
    {
    }
}
