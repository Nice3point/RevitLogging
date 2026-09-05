using Microsoft.Extensions.Logging;
using Nice3point.Revit.Logging.Tests.Fixtures;

namespace Nice3point.Revit.Logging.Tests;

/// <summary>
///     Covers what the logger hands to the journal and what it keeps from reaching the add-in.
/// </summary>
public sealed class RevitJournalLoggerTests : JournalLoggerTest
{
    [Test]
    public async Task Log_Record_WritesOneComment()
    {
        // Arrange
        var logger = CreateLogger(options => options.ApplicationName = "RevitAddin");

        // Act
        logger.LogError("Update service error");

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(Journal.Comments).HasSingleItem();
            await Assert.That(Journal.Single.Text).IsEqualTo($"RevitAddin_ERROR {{ {Category}: Update service error }}");
        }
    }

    [Test]
    public async Task Log_NullCharacter_ReplacesItWithSpace()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeCategory = false;
        });

        // Act
        logger.LogError("{Message}", "before\0after");

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo("RevitAddin_ERROR { before after }");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Log_TimestampOption_AsksRevitForTheStampAccordingly(bool includeTimestamp)
    {
        // Arrange
        var logger = CreateLogger(options => options.IncludeTimestamp = includeTimestamp);

        // Act
        logger.LogError("Update service error");

        // Assert
        await Assert.That(Journal.Single.TimeStamp).IsEqualTo(includeTimestamp);
    }

    [Test]
    public async Task Log_LevelNone_WritesNothing()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        logger.Log(LogLevel.None, "Update service error");

        // Assert
        await Assert.That(Journal.Comments).IsEmpty();
    }

    [Test]
    public async Task Log_JournalRejectsTheRecord_KeepsTheFailureFromTheCaller()
    {
        // Arrange
        var logger = CreateLogger();
        Journal.Failure = new InvalidOperationException("Journal rejected the record");

        // Act
        logger.LogError("Update service error");

        // Assert
        await Assert.That(Journal.Comments).IsEmpty();
    }

    [Test]
    public async Task Log_NullFormatter_Throws()
    {
        // Arrange
        var logger = CreateLogger();

        // Act & Assert
        await Assert.That(() => logger.Log<string>(LogLevel.Error, new EventId(0), "Update service error", null, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Log_OversizedRecord_TrimsTheBufferOfTheThread()
    {
        // Arrange
        var logger = CreateLogger(options => options.IncludeCategory = false);

        // Act
        logger.LogError("{Message}", new string('x', 8192));
        logger.LogError("Update service error");

        // Assert
        await Assert.That(Journal.Comments[1].Text).EndsWith("Update service error }");
    }

    [Test]
    public async Task BeginScope_WithoutScopeProvider_ReturnsNull()
    {
        // Arrange
        var logger = CreateLogger();

        // Act
        var scope = logger.BeginScope("Startup");

        // Assert
        await Assert.That(scope).IsNull();
    }
}
