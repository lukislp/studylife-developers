namespace StudyLifeDevelopers.Internal;

/// <summary>Settings of the server-to-server API StudyAtrium calls (/internal/*).</summary>
public sealed class InternalApiOptions
{
    public const string SectionName = "StudyLifeDevelopers";

    /// <summary>Must match StudyAtrium's own StudyLifeDevelopers:SharedSecret exactly. While it
    /// is empty, every /internal/* call is refused.</summary>
    public string SharedSecret { get; set; } = "";
}
