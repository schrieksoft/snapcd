FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
EXPOSE 8080

COPY out/snapcd-agent/ ./

# Application files stay root-owned so the unprivileged process cannot rewrite its own assemblies.
RUN chown root:root /app
USER $APP_UID

ENTRYPOINT ["dotnet", "SnapCd.Agent.dll"]
