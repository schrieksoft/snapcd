FROM ghcr.io/schrieksoft/base-image-snapcd-runner:1.2.2

WORKDIR /app
EXPOSE 8080

COPY out/snapcd-runner/ ./

# Application files stay root-owned so the unprivileged process cannot rewrite its own
# assemblies. runnerdata is the only path it writes, and deployments mount over it.
USER root
RUN chown root:root /app && install -d -o $APP_UID -g $APP_UID /app/runnerdata
USER $APP_UID

ENTRYPOINT ["dotnet", "SnapCd.Runner.dll"]
