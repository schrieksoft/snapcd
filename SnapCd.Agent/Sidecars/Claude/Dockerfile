FROM ghcr.io/schrieksoft/base-image-claude-sidecar:1.0.0

WORKDIR /app
USER app

# uv sync before the source copy, so a code-only change does not reinstall dependencies.
COPY --chown=app:app pyproject.toml uv.lock ./
RUN uv sync --no-install-project --frozen

COPY --chown=app:app . .

ENV PORT=7001
EXPOSE 7001
# Shell-form CMD so the operator-overridable PORT env is honoured at start time.
CMD ["sh", "-c", "uv run uvicorn main:app --host 0.0.0.0 --port ${PORT}"]
