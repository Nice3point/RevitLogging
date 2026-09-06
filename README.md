<p align="center">
    <picture>
        <source media="(prefers-color-scheme: dark)" width="750" srcset="https://github.com/user-attachments/assets/66176eea-222b-4016-82e8-35f373cfa1c4">
        <img alt="Nice3point.Revit.Logging" width="750" src="https://github.com/user-attachments/assets/1261e265-a862-46a4-9f13-008db33d49ed">
    </picture>
</p>

## Logging library for Revit

[![Nuget](https://img.shields.io/nuget/vpre/Nice3point.Revit.Logging?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://www.nuget.org/packages/Nice3point.Revit.Logging)
[![Downloads](https://img.shields.io/nuget/dt/Nice3point.Revit.Logging?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://www.nuget.org/packages/Nice3point.Revit.Logging)
[![Last Commit](https://img.shields.io/github/last-commit/Nice3point/RevitLogging/develop?style=for-the-badge&color=1A1A1A&labelColor=C42A2A)](https://github.com/Nice3point/RevitLogging/commits/develop)

Write logs for your Revit add-ins using [Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging).
The library adds the journal of the Revit session as a logging destination and writes every record there as a journal comment.

Log levels, filtering, scopes and message templates are handled by `Microsoft.Extensions.Logging`.

## Installation

You can install this library as a [NuGet package](https://www.nuget.org/packages/Nice3point.Revit.Logging).

The packages are compiled for specific versions of Revit. To support different versions of libraries in one project, use the `RevitVersion` property:

```xml

<PackageReference Include="Nice3point.Revit.Logging" Version="$(RevitVersion).*"/>
```

## Writing your first record

Start by adding the provider to the host of your application:

```c#
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddRevitJournal(Application.ControlledApplication);

        builder.Build().Start();
    }
}
```

`ServiceCollection` works the same way when your add-in has no host:

```c#
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddRevitJournal(Application.ControlledApplication));

        var serviceProvider = services.BuildServiceProvider();
    }
}
```

Inject `ILogger<T>` and write a record:

```c#
public class UpdateService(ILogger<UpdateService> logger)
{
    public void CheckUpdates()
    {
        logger.LogInformation("Checking updates");
    }
}
```

The record appears in the journal of the running session:

```text
'C 05-Sep-2026 22:48:59.703;   0:< RevitAddin_INFORMATION { RevitAddin.Services.UpdateService: Checking updates }
```

> [!NOTE]
> Revit stores journals in `%LocalAppData%\Autodesk\Revit\Autodesk Revit {version}\Journals`.

## Record format

The library writes the record, Revit writes the beginning of the line:

```text
' 0:< RevitAddin_WARNING { RevitAddin.Services.SettingsService: Settings file is missing }
'C 05-Sep-2026 22:48:59.703;   0:< RevitAddin_ERROR { RevitAddin.Services.UpdateService: Update service error }
```

A record has the following format:

```text
{ApplicationName}_{Level} { {Category}[{EventId}] => {Scope} => {Scope}: {Message}
{Exception} }
```

The braces bound the record. An exception occupies the lines that follow the message, and Revit opens each of them with an apostrophe:

```text
' 0:< RevitAddin_ERROR { RevitAddin.Services.UpdateService: Update service error
'System.Net.Http.HttpRequestException: Response status code does not indicate success: 403.
'   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode() }
```

> [!NOTE]
> Text you log needs no escaping. Revit comments out every line of a record, including a line that contains a journal command.

## Log levels

The provider alias is `RevitJournal`. Set the minimum level for the journal separately from the other providers:

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information"
        },
        "RevitJournal": {
            "LogLevel": {
                "Default": "Error"
            }
        }
    }
}
```

The same filter in code:

```c#
builder.Logging.AddFilter<RevitJournalLoggerProvider>(null, LogLevel.Error);
```

> [!IMPORTANT]
> A journal keeps the whole session and the user sends it to Autodesk with an error report. Keep the minimum level at `Warning` or `Error` in a release build.

## Options

| Option             | Default                                            | Description                                                    |
|--------------------|----------------------------------------------------|----------------------------------------------------------------|
| `ApplicationName`  | Name of the assembly that called `AddRevitJournal` | Opens the record token and identifies your add-in in a journal |
| `IncludeCategory`  | `true`                                             | Writes the category of the record                              |
| `IncludeEventId`   | `false`                                            | Writes the event id in square brackets after the category      |
| `IncludeScopes`    | `false`                                            | Writes the scopes of the record, joined by `=>`                |
| `IncludeTimestamp` | `true`                                             | Opens the journal line with the time stamp of the session      |
| `SingleLine`       | `false`                                            | Collapses the line breaks of the message and of the exception  |

Configure them when you add the provider:

```c#
builder.Logging.AddRevitJournal(Application.ControlledApplication, options =>
{
    options.ApplicationName = "RevitAddin";
    options.IncludeScopes = true;
});
```

Options also bind from the `Logging:RevitJournal` section:

```json
{
    "Logging": {
        "RevitJournal": {
            "IncludeScopes": true,
            "IncludeEventId": true
        }
    }
}
```

Configuration is applied first, and the `options` delegate overrides it.

### IncludeTimestamp

Revit writes the time stamp in the format the rest of the journal uses:

```text
'C 05-Sep-2026 22:48:59.703;   0:< RevitAddin_ERROR { ... }
```

Turn it off to shorten the line. The nearest stamped line above then dates the record:

```text
' 0:< RevitAddin_ERROR { ... }
```

## Scopes

`IncludeScopes` writes the scopes of the record, from the outermost one:

```c#
using (logger.BeginScope("Startup"))
using (logger.BeginScope("Document {Title}", document.Title))
{
    logger.LogError("Update service error");
}
```

```text
' 0:< RevitAddin_ERROR { RevitAddin.Services.UpdateService => Startup => Document Snowdon Towers: Update service error }
```

## Multithreading

A record is written on the thread that logged it, both inside and outside the Revit API context.
Wrap nothing in an external event and marshal nothing onto the Revit thread:

```c#
await Task.Run(() =>
{
    logger.LogError("Update service error");
});
```

Revit serializes the writes, and records never mix with each other.

## Custom formatter

Inherit `RevitJournalFormatter` to write your own record format:

```c#
public class MessageOnlyFormatter : RevitJournalFormatter
{
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, StringBuilder record)
    {
        record.Append(logEntry.LogLevel)
            .Append(' ')
            .Append(logEntry.Formatter(logEntry.State, logEntry.Exception));
    }
}
```

Register it after the provider:

```c#
builder.Logging.AddRevitJournal(Application.ControlledApplication);
builder.Logging.AddRevitJournalFormatter<MessageOnlyFormatter>();
```

The `record` buffer is empty on entry and reused between records. Leave it empty to discard a record.
