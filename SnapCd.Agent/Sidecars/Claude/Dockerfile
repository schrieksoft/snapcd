FROM python:3.12-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm git ca-certificates curl \
    && install -d -m 0755 /etc/apt/keyrings \
    && curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg -o /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" > /etc/apt/sources.list.d/github-cli.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends gh \
    && rm -rf /var/lib/apt/lists/*
RUN npm install -g @anthropic-ai/claude-code

COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

WORKDIR /app
COPY pyproject.toml ./
RUN uv sync --no-install-project

COPY . .

# The Claude CLI refuses --dangerously-skip-permissions (the sidecar's bypassPermissions mode) when
# running as root, so the container must run as a non-root user.
RUN useradd --create-home app && chown -R app:app /app
USER app

ENV PORT=7001
EXPOSE 7001
# Shell-form CMD so the operator-overridable PORT env is honoured at start time.
CMD ["sh", "-c", "uv run uvicorn main:app --host 0.0.0.0 --port ${PORT}"]
