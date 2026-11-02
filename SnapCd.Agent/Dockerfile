# Build stage. Build context is applications/snapcd (so the sibling projects are available).
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy the referenced projects + the agent project.
COPY SnapCd.Contracts/ SnapCd.Contracts/
COPY SnapCd.Utils/ SnapCd.Utils/
COPY SnapCd.Agent/ SnapCd.Agent/

WORKDIR /src/SnapCd.Agent
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SnapCd.Agent.dll"]
