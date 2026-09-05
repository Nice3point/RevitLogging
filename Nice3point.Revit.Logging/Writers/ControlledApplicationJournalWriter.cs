using Autodesk.Revit.ApplicationServices;

namespace Nice3point.Revit.Logging.Writers;

/// <summary>
///     Writes through the controlled application an add-in receives while Revit starts.
/// </summary>
internal sealed class ControlledApplicationJournalWriter(ControlledApplication application) : IJournalWriter
{
    public void WriteComment(string comment, bool timeStamp)
    {
        application.WriteJournalComment(comment, timeStamp);
    }
}
