FROM ghcr.io/schrieksoft/base-image-snapcd-runner:1.2.2

WORKDIR /app
EXPOSE 8080

# The base image already runs as its non-root user, so the copy must grant it ownership.
COPY --chown=$APP_UID:$APP_UID out/snapcd-runner/ ./
RUN chmod +x /app/SnapCd.Runner

ENTRYPOINT ["./SnapCd.Runner"]
