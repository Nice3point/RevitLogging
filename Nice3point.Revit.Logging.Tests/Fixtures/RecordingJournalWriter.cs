using Nice3point.Revit.Logging.Writers;

namespace Nice3point.Revit.Logging.Tests.Fixtures;

/// <summary>
///     Collects the comments a logger produces instead of writing them to Revit.
/// </summary>
internal sealed class RecordingJournalWriter : IJournalWriter
{
    private readonly List<JournalComment> _comments = [];

    /// <summary>
    ///     Gets the comments the logger produced, in the order it produced them.
    /// </summary>
    public IReadOnlyList<JournalComment> Comments => _comments;

    /// <summary>
    ///     Gets the single comment the logger produced.
    /// </summary>
    /// <exception cref="InvalidOperationException">The logger produced no comment, or more than one.</exception>
    public JournalComment Single => _comments.Single();

    /// <summary>
    ///     Gets or sets the failure every write raises, or <see langword="null" /> when a write succeeds.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <inheritdoc />
    public void WriteComment(string comment, bool timeStamp)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        _comments.Add(new JournalComment(comment, timeStamp));
    }
}

/// <summary>
///     Represents one comment a logger handed to the journal.
/// </summary>
/// <param name="Text">The text of the comment.</param>
/// <param name="TimeStamp">Whether the comment opens with the time stamp of the journal.</param>
internal sealed record JournalComment(string Text, bool TimeStamp);
