# Preview base image — switch to mcr.microsoft.com/dotnet/aspnet:10.0 at .NET 10 GA.
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0.0-rc.2-noble
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=http://*:80

COPY out/snapcd-server/ ./

ENTRYPOINT ["dotnet", "SnapCd.Server.SelfHosted.dll"]
