FROM ghcr.io/schrieksoft/base-image-snapcd-runner:1.0.2

EXPOSE 80
EXPOSE 443

WORKDIR /app
ENV ASPNETCORE_URLS=http://*:80

COPY out/snapcd-runner/ ./
RUN chmod +x /app/SnapCd.Runner

ENTRYPOINT ["./SnapCd.Runner"]
