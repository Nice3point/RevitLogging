using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Logging.Tests;

/// <summary>
///     Covers the journal of a running Revit session, the destination the provider is built for.
/// </summary>
/// <remarks>
///     Revit owns the beginning of every journal line and the escaping of every line after the first.
///     Each test writes under an application name of its own and reads the record back out of the file Revit is recording.
/// </remarks>
public sealed class RevitJournalTests : RevitApiTest
{
    private const string Category = "RevitAddin.Services.UpdateService";

    [Test]
    public async Task Log_Record_ReachesTheJournalOfTheSession()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application);

        // Act
        logger.LogError("Update service error");

        // Assert
        var journal = await ReadJournalAsync();
        await Assert.That(journal).Contains($"{application}_ERROR {{ {Category}: Update service error }}");
    }

    [Test]
    public async Task Log_StampedRecord_OpensTheLineWithTheJournalTimeStamp()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application);

        // Act
        logger.LogError("Update service error");

        // Assert
        var line = await FindLineAsync(application);
        using (Assert.Multiple())
        {
            await Assert.That(line).StartsWith("'C ");
            await Assert.That(line).Contains($"0:< {application}_ERROR {{ ");
        }
    }

    [Test]
    public async Task Log_UnstampedRecord_OpensTheLineWithTheCommentMarkerAlone()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application, options => options.IncludeTimestamp = false);

        // Act
        logger.LogError("Update service error");

        // Assert
        var line = await FindLineAsync(application);
        await Assert.That(line).StartsWith($"' 0:< {application}_ERROR {{ ");
    }

    [Test]
    public async Task Log_Exception_ContinuesEveryFurtherLineWithAnApostrophe()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application);

        // Act
        logger.LogError(new InvalidOperationException("Rate limit exceeded"), "Update service error");

        // Assert
        var journal = await ReadJournalAsync();
        await Assert.That(journal)
            .Contains($"{application}_ERROR {{ {Category}: Update service error\n'System.InvalidOperationException: Rate limit exceeded }}");
    }

    [Test]
    public async Task Log_RecordCarryingAJournalCommand_LeavesTheCommandCommentedOut()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application);

        // Act
        logger.LogError("{Record}", $"before{Environment.NewLine}Jrn.Command \"Internal\"{Environment.NewLine}after");

        // Assert
        var journal = await ReadJournalAsync();
        await Assert.That(journal).Contains("\n'Jrn.Command \"Internal\"\n");
    }

    [Test]
    public async Task Log_FromBackgroundThread_ReachesTheJournalWithoutMarshalling()
    {
        // Arrange
        var application = CreateApplicationName();
        var logger = CreateLogger(application);
        var revitThread = Environment.CurrentManagedThreadId;

        // Act
        var loggingThread = await Task.Run(() =>
        {
            logger.LogError("Update service error");
            return Environment.CurrentManagedThreadId;
        });

        // Assert
        var journal = await ReadJournalAsync();
        using (Assert.Multiple())
        {
            await Assert.That(loggingThread).IsNotEqualTo(revitThread);
            await Assert.That(journal).Contains($"{application}_ERROR {{ {Category}: Update service error }}");
        }
    }

    private static string CreateApplicationName()
    {
        return $"Probe{Guid.NewGuid():N}";
    }

    private ILogger CreateLogger(string applicationName, Action<RevitJournalLoggerOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddRevitJournal(Application, options =>
        {
            options.ApplicationName = applicationName;
            configure?.Invoke(options);
        }));

        return services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger(Category);
    }

    /// <summary>
    ///     Reads the journal Revit is recording while it still holds the file open.
    /// </summary>
    private static async Task<string> ReadJournalAsync()
    {
#if NET
        await using var stream = new FileStream(Application.RecordingJournalFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#else
        using var stream = new FileStream(Application.RecordingJournalFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#endif
        using var reader = new StreamReader(stream);

        var journal = await reader.ReadToEndAsync();
        return journal.Replace("\r\n", "\n");
    }

    private static async Task<string> FindLineAsync(string applicationName)
    {
        var journal = await ReadJournalAsync();

        return journal
            .Split('\n')
            .Single(line => line.Contains(applicationName, StringComparison.Ordinal));
    }
}
