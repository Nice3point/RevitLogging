using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nice3point.Revit.Logging.Formatters;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Logging.Tests;

/// <summary>
///     Covers what <c>AddRevitJournal</c> puts into the container.
/// </summary>
/// <remarks>The container carries no configuration section, which is the shape an add-in registering the provider by hand builds.</remarks>
public sealed class RevitLoggingRegistrationTests : RevitApiTest
{
    [Test]
    public async Task AddRevitJournal_WithoutConfigurationSection_ResolvesTheProvider()
    {
        // Arrange
        await using var services = BuildProvider();

        // Act
        var providers = services.GetServices<ILoggerProvider>().ToArray();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(providers).HasSingleItem();
            await Assert.That(providers[0]).IsTypeOf<RevitJournalLoggerProvider>();
        }
    }

    [Test]
    public async Task AddRevitJournal_CalledTwice_RegistersOneProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddRevitJournal(Application);
            logging.AddRevitJournal(Application);
        });

        // Act
        await using var provider = services.BuildServiceProvider();

        // Assert
        await Assert.That(provider.GetServices<ILoggerProvider>()).HasSingleItem();
    }

    [Test]
    public async Task AddRevitJournal_DefaultOptions_NamesTheApplicationAfterTheCallingAssembly()
    {
        // Arrange
        await using var services = BuildProvider();

        // Act
        var options = services.GetRequiredService<IOptionsMonitor<RevitJournalLoggerOptions>>();

        // Assert
        await Assert.That(options.CurrentValue.ApplicationName).IsEqualTo(typeof(RevitLoggingRegistrationTests).Assembly.GetName().Name);
    }

    [Test]
    public async Task AddRevitJournal_ExplicitOptions_OverrideTheDefaults()
    {
        // Arrange
        await using var services = BuildProvider(options => options.ApplicationName = "RevitAddin");

        // Act
        var options = services.GetRequiredService<IOptionsMonitor<RevitJournalLoggerOptions>>();

        // Assert
        await Assert.That(options.CurrentValue.ApplicationName).IsEqualTo("RevitAddin");
    }

    [Test]
    public async Task AddRevitJournalFormatter_CustomFormatter_ReplacesTheDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.AddRevitJournal(Application);
            logging.AddRevitJournalFormatter<MessageOnlyFormatter>();
        });

        // Act
        await using var provider = services.BuildServiceProvider();

        // Assert
        await Assert.That(provider.GetRequiredService<RevitJournalFormatter>()).IsTypeOf<MessageOnlyFormatter>();
    }

    [Test]
    public async Task AddRevitJournal_NullApplication_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        await Assert.That(() => services.AddLogging(logging => logging.AddRevitJournal((Autodesk.Revit.ApplicationServices.Application)null!)))
            .Throws<ArgumentNullException>();
    }

    private ServiceProvider BuildProvider(Action<RevitJournalLoggerOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddRevitJournal(Application, configure));

        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Renders the message of a record and nothing else.
    /// </summary>
    private sealed class MessageOnlyFormatter : RevitJournalFormatter
    {
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, StringBuilder record)
        {
            record.Append(logEntry.Formatter(logEntry.State, logEntry.Exception));
        }
    }
}
