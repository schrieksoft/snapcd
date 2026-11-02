# Build stage. Build context is applications/snapcd (so the sibling SnapCd.Contracts is available).
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy the referenced contracts project + the agent project.
COPY SnapCd.Contracts/ SnapCd.Contracts/
COPY SnapCd.Agent/ SnapCd.Agent/

WORKDIR /src/SnapCd.Agent
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app .

USER $APP_UID

ENTRYPOINT ["dotnet", "SnapCd.Agent.dll"]
