using Microsoft.Extensions.Logging;
using Nice3point.Revit.Logging.Tests.Fixtures;

namespace Nice3point.Revit.Logging.Tests;

/// <summary>
///     Covers the record the default formatter renders.
/// </summary>
/// <remarks>The record takes the block shape Revit writes its own <c>API_SUCCESS</c> and <c>API_ERROR</c> comments in.</remarks>
public sealed class RevitJournalFormatterTests : JournalLoggerTest
{
    [Test]
    public async Task Write_Record_OpensTheBlockWithTheApplicationAndTheLevel()
    {
        // Arrange
        var logger = CreateLogger(options => options.ApplicationName = "RevitAddin");

        // Act
        logger.LogInformation("Update service error");

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo($"RevitAddin_INFORMATION {{ {Category}: Update service error }}");
    }

    [Test]
    [Arguments(LogLevel.Trace, "TRACE")]
    [Arguments(LogLevel.Debug, "DEBUG")]
    [Arguments(LogLevel.Information, "INFORMATION")]
    [Arguments(LogLevel.Warning, "WARNING")]
    [Arguments(LogLevel.Error, "ERROR")]
    [Arguments(LogLevel.Critical, "CRITICAL")]
    public async Task Write_EveryLevel_NamesTheLevelInTheToken(LogLevel logLevel, string expected)
    {
        // Arrange
        var logger = CreateLogger(options => options.ApplicationName = "RevitAddin");

        // Act
        logger.Log(logLevel, "Update service error");

        // Assert
        await Assert.That(Journal.Single.Text).StartsWith($"RevitAddin_{expected} {{ ");
    }

    [Test]
    public async Task Write_CategoryDisabled_KeepsTheMessageAlone()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeCategory = false;
        });

        // Act
        logger.LogInformation("Update service error");

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo("RevitAddin_INFORMATION { Update service error }");
    }

    [Test]
    public async Task Write_EventIdEnabled_FollowsTheCategoryWithTheEventId()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeEventId = true;
        });

        // Act
        logger.Log(LogLevel.Information, new EventId(42), "Update service error");

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo($"RevitAddin_INFORMATION {{ {Category}[42]: Update service error }}");
    }

    [Test]
    public async Task Write_ScopesEnabled_JoinsTheScopesFromTheOutermost()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeScopes = true;
        });

        logger.ScopeProvider = new LoggerExternalScopeProvider();

        // Act
        using (logger.BeginScope("Startup"))
        using (logger.BeginScope("Document {Title}", "Snowdon Towers"))
        {
            logger.LogInformation("Update service error");
        }

        // Assert
        await Assert.That(Journal.Single.Text)
            .IsEqualTo($"RevitAddin_INFORMATION {{ {Category} => Startup => Document Snowdon Towers: Update service error }}");
    }

    [Test]
    public async Task Write_ScopesEnabledWithoutCategory_OmitsTheLeadingSeparator()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeCategory = false;
            options.IncludeScopes = true;
        });

        logger.ScopeProvider = new LoggerExternalScopeProvider();

        // Act
        using (logger.BeginScope("Startup"))
        {
            logger.LogInformation("Update service error");
        }

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo("RevitAddin_INFORMATION { Startup: Update service error }");
    }

    [Test]
    public async Task Write_ScopesDisabled_IgnoresThePushedScopes()
    {
        // Arrange
        var logger = CreateLogger(options => options.ApplicationName = "RevitAddin");
        logger.ScopeProvider = new LoggerExternalScopeProvider();

        // Act
        using (logger.BeginScope("Startup"))
        {
            logger.LogInformation("Update service error");
        }

        // Assert
        await Assert.That(Journal.Single.Text).IsEqualTo($"RevitAddin_INFORMATION {{ {Category}: Update service error }}");
    }

    [Test]
    public async Task Write_Exception_BreaksTheLineBeforeItAndClosesTheBlock()
    {
        // Arrange
        var logger = CreateLogger(options => options.ApplicationName = "RevitAddin");
        var exception = new InvalidOperationException("Rate limit exceeded");

        // Act
        logger.LogError(exception, "Update service error");

        // Assert
        await Assert.That(Journal.Single.Text)
            .IsEqualTo($"RevitAddin_ERROR {{ {Category}: Update service error{Environment.NewLine}System.InvalidOperationException: Rate limit exceeded }}");
    }

    [Test]
    public async Task Write_SingleLine_KeepsTheMessageAndTheExceptionOnOneLine()
    {
        // Arrange
        var logger = CreateLogger(options =>
        {
            options.ApplicationName = "RevitAddin";
            options.IncludeCategory = false;
            options.SingleLine = true;
        });

        var exception = new InvalidOperationException("Rate limit exceeded");

        // Act
        logger.LogError(exception, "{Message}", $"first{Environment.NewLine}second");

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(Journal.Single.Text).DoesNotContain(Environment.NewLine);
            await Assert.That(Journal.Single.Text)
                .IsEqualTo("RevitAddin_ERROR { first second System.InvalidOperationException: Rate limit exceeded }");
        }
    }
}
