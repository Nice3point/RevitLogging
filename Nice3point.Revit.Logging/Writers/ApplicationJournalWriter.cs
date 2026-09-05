using Autodesk.Revit.ApplicationServices;

namespace Nice3point.Revit.Logging.Writers;

/// <summary>
///     Writes through the database level Revit application.
/// </summary>
internal sealed class ApplicationJournalWriter(Application application) : IJournalWriter
{
    public void WriteComment(string comment, bool timeStamp)
    {
        application.WriteJournalComment(comment, timeStamp);
    }
}
