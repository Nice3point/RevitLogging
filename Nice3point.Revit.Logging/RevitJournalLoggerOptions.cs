namespace Nice3point.Revit.Logging;

/// <summary>
///     Represents the options for the journal logger named 'RevitJournal'.
/// </summary>
/// <remarks>
///     The provider alias is <c>RevitJournal</c>. Configuration binds these options from the <c>Logging:RevitJournal</c> section
///     once <c>AddConfiguration</c> has been called on the logging builder.
/// </remarks>
/// <example>
///     <code lang="json">
/// {
///   "Logging": {
///     "RevitJournal": {
///       "LogLevel": {
///         "Default": "Error"
///       },
///       "IncludeScopes": true
///     }
///   }
/// }
/// </code>
/// </example>
[PublicAPI]
public sealed class RevitJournalLoggerOptions
{
    /// <summary>
    ///     Gets or sets the name opening the record token, identifying the add-in among the entries of the whole session.
    /// </summary>
    /// <remarks>Defaults to the name of the assembly that called <c>AddRevitJournal</c>.</remarks>
    /// <example>
    ///     <c>RevitAddin</c> opens the record <c>RevitAddin_ERROR { ... }</c>.
    /// </example>
    public string ApplicationName { get; set; } = "RevitAddin";

    /// <summary>
    ///     Gets or sets a value that indicates whether the category of the record is written.
    /// </summary>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value that indicates whether the event id of the record is written in square brackets after the category.
    /// </summary>
    public bool IncludeEventId { get; set; }

    /// <summary>
    ///     Gets or sets a value that indicates whether the scopes the record was created in are written, joined by <c>=&gt;</c>.
    /// </summary>
    public bool IncludeScopes { get; set; }

    /// <summary>
    ///     Gets or sets a value that indicates whether Revit opens the journal comment with a time stamp.
    /// </summary>
    /// <remarks>
    ///     Revit writes the stamp in the format the rest of the journal uses, <c>'C 05-Sep-2026 22:45:23.749;</c>.
    ///     Without it the record carries no time, and the nearest stamped line above dates it.
    /// </remarks>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value that indicates whether the line breaks of the message and of the exception are collapsed into spaces, keeping the record on one journal line.
    /// </summary>
    /// <remarks>
    ///     Revit splits a comment on every line break and opens each following line with an apostrophe.
    ///     Collapse the record when the tool that reads the journal expects one line per entry.
    /// </remarks>
    public bool SingleLine { get; set; }
}
