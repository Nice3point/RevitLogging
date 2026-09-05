using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nice3point.Revit.Logging.Formatters;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Logging.Tests.Fixtures;

/// <summary>
///     Provides a logger writing to a collected journal instead of the running Revit session.
/// </summary>
/// <remarks>The default formatter renders every record, and the options carry the defaults of the provider until a test overrides them.</remarks>
public abstract class JournalLoggerTest : RevitApiTest
{
    /// <summary>
    ///     Category every logger of the fixture writes under.
    /// </summary>
    protected const string Category = "RevitAddin.Services.UpdateService";

    /// <summary>
    ///     Gets the journal the logger of the most recent <see cref="CreateLogger" /> call wrote to.
    /// </summary>
    internal RecordingJournalWriter Journal { get; } = new();

    /// <summary>
    ///     Creates a logger over the collected journal.
    /// </summary>
    /// <param name="configure">An optional action to configure the <see cref="RevitJournalLoggerOptions" />.</param>
    /// <returns>The logger, writing under <see cref="Category" />.</returns>
    internal RevitJournalLogger CreateLogger(Action<RevitJournalLoggerOptions>? configure = null)
    {
        var monitor = CreateOptions(configure);
        return new RevitJournalLogger(Category, Journal, new DefaultRevitJournalFormatter(monitor), monitor);
    }

    /// <summary>
    ///     Renders one record through the default formatter, without a logging factory.
    /// </summary>
    /// <param name="configure">An action to configure the <see cref="RevitJournalLoggerOptions" />.</param>
    /// <param name="write">The record to render.</param>
    /// <returns>The text of the journal comment.</returns>
    internal string Render(Action<RevitJournalLoggerOptions> configure, Action<ILogger> write)
    {
        var logger = CreateLogger(configure);
        write(logger);

        return Journal.Single.Text;
    }

    private static IOptionsMonitor<RevitJournalLoggerOptions> CreateOptions(Action<RevitJournalLoggerOptions>? configure)
    {
        var options = new RevitJournalLoggerOptions();
        configure?.Invoke(options);

        return new StaticOptionsMonitor<RevitJournalLoggerOptions>(options);
    }
}

/// <summary>
///     Serves one options instance that never changes.
/// </summary>
/// <param name="value">The options every consumer reads.</param>
internal sealed class StaticOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
{
    /// <inheritdoc />
    public TOptions CurrentValue { get; } = value;

    /// <inheritdoc />
    public TOptions Get(string? name) => CurrentValue;

    /// <inheritdoc />
    public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
}
