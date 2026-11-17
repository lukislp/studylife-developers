namespace StudyLifeDevelopers.Services;

/// <summary>
/// Local mirrors of the studylife repo's own Dtos.cs shapes (DeveloperClientDto/
/// CreateDeveloperClientRequestDto/UpdateDeveloperClientRequestDto) - no shared project
/// reference across repos, so these are hand-kept in sync. Property names must match exactly
/// what studylife's /api/developer/clients endpoints send/expect (camelCase JSON, ASP.NET
/// Core's web default) - see StudyLifeApiClient's JsonSerializerOptions.
/// </summary>
public class DeveloperClientDto
{
    public string ClientId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AllowedRedirectUris { get; set; } = new();
    public List<string> RequestedScopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateDeveloperClientRequestDto
{
    public string ClientId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AllowedRedirectUris { get; set; } = new();
    public List<string> RequestedScopes { get; set; } = new();
}

public class UpdateDeveloperClientRequestDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AllowedRedirectUris { get; set; } = new();
    public List<string> RequestedScopes { get; set; } = new();
}

/// <summary>Response of GET /api/auth/whoami (studylife's WhoamiResponseDto).</summary>
public class WhoamiResponseDto
{
    public int UserId { get; set; }
    public string Credential { get; set; } = "";
}

/// <summary>Body of POST /api/auth/assertion-exchange (studylife's
/// GenericAssertionExchangeRequestDto).</summary>
public class AssertionExchangeRequestDto
{
    public string ClientId { get; set; } = "";
    public string Assertion { get; set; } = "";
    public string? CodeVerifier { get; set; }
}

/// <summary>Response of POST /api/auth/assertion-exchange (studylife's
/// GenericAssertionExchangeResponseDto).</summary>
public class AssertionExchangeResponseDto
{
    public int UserId { get; set; }
    public string ApiKey { get; set; } = "";
}
