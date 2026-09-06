# Nice3point.Revit.Logging

Nice3point.Revit.Logging is a NuGet package that writes `Microsoft.Extensions.Logging` records to the journal of the running Revit session.
It adds the journal as a logging provider. Log levels, filtering, scopes and message templates are handled by `Microsoft.Extensions.Logging`.

## Non-negotiables

* Revit accepts `WriteJournalComment` from any thread and from outside the API context. A record is written on the thread that logged it, and the provider marshals nothing.
* A failure of a journal write never reaches the caller. The logger swallows it.
* The Revit API stays in `Writers/`. The formatter, the logger and the options reference no Revit type.
* The public surface and the registration follow the Microsoft logging providers.
* Never break the public surface. Deprecate a renamed member with `[Obsolete]`, name the replacement, and keep the member functional.
* Every type compiles under every supported configuration.
* The `Microsoft.Extensions` versions are a floor, held at the oldest supported LTS and excluded from Renovate in `renovate.json`. Raise one only for an API the provider needs.
* Confirm an unfamiliar Revit or .NET API before use through official docs or `gh` (`gh api`, `gh search code`).
* A public-surface change updates `README.md`, `CHANGELOG.md` and the XML docs in the same commit.

## Journal behavior

The package rests on what Revit does to the text of a comment. Each of the following is covered by a test in `RevitJournalTests`.

* Revit prepends `' <depth>:< ` to the comment, and `'C <dd-MMM-yyyy HH:mm:ss.fff>;   <depth>:< ` when a time stamp is requested. It appends a trailing space.
* Revit splits the comment on `\r\n`, `\n` and a bare `\r`, and opens every line after the first with a bare apostrophe. A journal command inside a record stays commented out.
* A null character truncates the comment at the point it appears. `RevitJournalLogger` replaces it.
* No length limit applies. A comment of one megabyte reaches the journal whole.
* `WriteJournalComment(null)` throws `Autodesk.Revit.Exceptions.ArgumentNullException`.

## Repository map

* `Nice3point.Revit.Logging/` — the provider, packed as a NuGet package. `RevitLoggingRegistration` is the entry point users call.
* `Nice3point.Revit.Logging.Tests/` — TUnit tests. Every test runs inside a Revit session opened by the injector, on the Revit thread the assembly-level executor of `TestsConfiguration.cs` marshals it onto. `RevitJournalTests` asserts against the file Revit is recording.
* `build/` — the ModularPipelines build for packing and publishing.
* Root — `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `README.md`, `CHANGELOG.md`, CI workflows.

## Build and verify

* Build: `dotnet build -c Release.R##`, where the `R##` suffix is the Revit year (`R27` targets Revit 2027).
* Test: `dotnet run --project Nice3point.Revit.Logging.Tests -c Release.R##`; requires a matching licensed Revit installation.
