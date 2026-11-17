namespace StudyLifeDevelopers.Services;

/// <summary>The outbound side: the key store and the two typed clients for the paired
/// StudyAtrium instance (StudyLife:BaseUrl, trusting StudyLife:CaCertPath).</summary>
public static class StudyLifeServiceCollectionExtensions
{
    public static IServiceCollection AddStudyLifeClients(this IServiceCollection services)
    {
        services.AddSingleton<KeyStore>();
        services.AddHttpClient<StudyLifeApiClient>(ConfigureBaseAddress)
            .ConfigurePrimaryHttpMessageHandler(sp => StudyLifeCaTrust.CreateHandler(sp.GetRequiredService<IConfiguration>()));
        services.AddHttpClient<StudyLifeSignInClient>(ConfigureBaseAddress)
            .ConfigurePrimaryHttpMessageHandler(sp => StudyLifeCaTrust.CreateHandler(sp.GetRequiredService<IConfiguration>()));
        return services;
    }

    private static void ConfigureBaseAddress(IServiceProvider services, HttpClient http)
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["StudyLife:BaseUrl"];
        if (!string.IsNullOrEmpty(baseUrl)) http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }
}
