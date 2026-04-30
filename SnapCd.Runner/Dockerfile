# Preview base image — switch to mcr.microsoft.com/dotnet/aspnet:10.0 at .NET 10 GA.
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-preview-bookworm-slim

EXPOSE 80
EXPOSE 443

RUN apt-get update && \
    apt-get install -y --no-install-recommends git openssh-client wget curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://*:80

COPY out/snapcd-runner/ ./
RUN chmod +x /app/SnapCd.Runner

ENTRYPOINT ["./SnapCd.Runner"]
