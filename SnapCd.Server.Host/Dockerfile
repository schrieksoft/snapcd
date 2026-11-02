FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080

COPY --chown=$APP_UID:$APP_UID out/snapcd-server/ ./

USER $APP_UID

ENTRYPOINT ["dotnet", "SnapCd.Server.Host.dll"]
