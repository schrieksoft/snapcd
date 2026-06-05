# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""Sidecar configuration, sourced from environment variables."""
from __future__ import annotations

import json
from functools import lru_cache
from typing import Any

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(extra="ignore")

    snapcd_base_url: str = Field(alias="SNAPCD_BASE_URL")

    external_mcp_config: str = Field(default="{}", alias="EXTERNAL_MCP_CONFIG")

    model: str = Field(default="claude-opus-4-7", alias="CLAUDE_MODEL")
    max_turns: int = Field(default=24, alias="CLAUDE_MAX_TURNS")
    permission_mode: str = Field(default="bypassPermissions", alias="CLAUDE_PERMISSION_MODE")

    host: str = Field(default="0.0.0.0", alias="HOST")
    port: int = Field(default=7001, alias="PORT")

    def external_mcp_servers(self) -> dict[str, Any]:
        return json.loads(self.external_mcp_config or "{}")


@lru_cache
def get_settings() -> Settings:
    return Settings()
