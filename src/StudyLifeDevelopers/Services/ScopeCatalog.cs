namespace StudyLifeDevelopers.Services;

/// <summary>
/// Mirrors ApiKeyScopes.PubliclyGrantable in the studylife repo (src/StudyLife.Server/Auth/
/// ApiKeyScopes.cs) and schema/known-scopes.json in studylife-marketplace - the only scopes a
/// dynamically registered OAuthClientEntity may ever request. Auth.Whoami is deliberately
/// excluded: every registered client gets it automatically, it is never requested. Kept in sync
/// by hand across all three places; update together whenever a new scope is exposed.
/// </summary>
public static class ScopeCatalog
{
    public sealed record Scope(string Id, string Label);

    public static readonly IReadOnlyList<Scope> All =
    [
        new("Notes.GetAll", "Read notes"),
        new("Notes.Search", "Search notes"),
        new("Notes.Create", "Create notes"),
        new("Notes.Update", "Edit notes"),
        new("Notes.Delete", "Delete notes"),
        new("Sessions.GetAll", "Read sessions"),
        new("Sessions.GetHistory", "Read session history"),
        new("Sessions.Create", "Create sessions"),
        new("Sessions.Update", "Edit sessions"),
        new("Sessions.Delete", "Delete sessions"),
        new("CourseGoals.GetAll", "Read course goals"),
        new("CourseGoals.Save", "Set course goals"),
        new("CourseGoals.Delete", "Delete course goals"),
        new("TimerState.Get", "Read live timer state"),
        new("Courses.GetAll", "Read the course catalog"),
        new("StudyPrograms.GetAll", "Read study programs"),
        new("StudyPrograms.Get", "Read a study program's detail"),
        new("Metrics.GetSummary", "Read metrics summary"),
        new("WebhooksProxy.List", "List webhook registrations"),
        new("WebhooksProxy.Create", "Create webhook registrations"),
        new("WebhooksProxy.Delete", "Delete webhook registrations"),
    ];
}
