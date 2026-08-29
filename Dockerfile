FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/StudyLifeDevelopers/StudyLifeDevelopers.csproj src/StudyLifeDevelopers/
RUN dotnet restore src/StudyLifeDevelopers/StudyLifeDevelopers.csproj
COPY src/StudyLifeDevelopers/ src/StudyLifeDevelopers/
RUN dotnet publish src/StudyLifeDevelopers/StudyLifeDevelopers.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
# DataDir (KeyStore's single-file JSON store) must exist and be owned by a non-root user
# before a volume mounts over it - same reasoning as every other Dockerfile in this
# ecosystem: a freshly-provisioned volume mount is otherwise root-owned and the non-root
# container gets "Permission denied". Reuses the "app" user (uid 1654) the aspnet base image
# already ships, instead of creating a new one - matches studylife's own k8s
# securityContext.runAsUser: 1654 for the exact same base image family.
RUN mkdir -p /app/data && chown -R app:app /app
USER app
COPY --from=build --chown=app:app /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StudyLifeDevelopers.dll"]
