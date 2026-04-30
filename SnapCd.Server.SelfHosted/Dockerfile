FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://*:80

COPY out/snapcd-server/ ./

ENTRYPOINT ["dotnet", "SnapCd.Server.SelfHosted.dll"]
