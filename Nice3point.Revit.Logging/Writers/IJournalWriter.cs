namespace Nice3point.Revit.Logging.Writers;

/// <summary>
///     Defines a contract that writes a comment to the journal of the running Revit session.
/// </summary>
/// <remarks>An implementation registered before <c>AddRevitJournal</c> receives every record in place of the Revit application.</remarks>
[PublicAPI]
public interface IJournalWriter
{
    /// <summary>
    ///     Writes a comment to the journal.
    /// </summary>
    /// <param name="comment">The text of the comment. Revit opens every line after the first with an apostrophe.</param>
    /// <param name="timeStamp">Whether the comment opens with the time stamp of the journal.</param>
    void WriteComment(string comment, bool timeStamp);
}
