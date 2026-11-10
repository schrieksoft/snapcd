#!/usr/bin/env python3
# SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
# Copyright (c) 2026 Karl Schriek / Schrieksoft.
# No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
# embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
# system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
# Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
# for terms covering either use.

"""Mints a federated assertion for local testing and writes it where the Runner reads it.

The preseeded debug Federated Credential trusts this Server as the issuer, because the Server
publishes a discovery document and a JWKS of its own. So an assertion signed with the Server's
development signing key validates against the Server's own published key, and no cluster or
second process is involved.

The key is the development one in SnapCd.Server.Host/appsettings.json. It is a published sample
key with no value outside local development.

Usage:
    scripts/mint-federated-assertion.py                 # writes ~/.snapcd/runner-federated-token
    scripts/mint-federated-assertion.py --print         # also prints the token
    scripts/mint-federated-assertion.py --lifetime 300  # seconds, default 3600

Needs `pip install pyjwt cryptography`.
"""

import argparse
import json
import os
import re
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
APPSETTINGS = REPO / "SnapCd.Server.Host" / "appsettings.json"

# Must match the preseeded credential in DebugDataSeeder.SeedDebugFederatedRunner.
SUBJECT = "system:serviceaccount:snapcd:debug-runner"
AUDIENCE = "snapcd"
DEFAULT_OUTPUT = Path.home() / ".snapcd" / "runner-federated-token"


def load_appsettings() -> dict:
    raw = APPSETTINGS.read_text()
    # appsettings.json carries // comments, which json.loads rejects. Strip them outside strings.
    stripped = re.sub(r'(?<!:)//[^\n"]*$', "", raw, flags=re.MULTILINE)
    return json.loads(stripped)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lifetime", type=int, default=3600, help="token lifetime in seconds")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--print", action="store_true", dest="show")
    parser.add_argument("--subject", default=SUBJECT)
    parser.add_argument("--audience", default=AUDIENCE)
    args = parser.parse_args()

    try:
        import jwt  # PyJWT
    except ImportError:
        print("PyJWT is required: pip install pyjwt cryptography", file=sys.stderr)
        return 1

    settings = load_appsettings()
    private_key = settings["OpenIdConnect"]["TokenSigning"]["RsaPrivateKey"]
    host = settings["Server"]["Host"].rstrip("/")
    issuer = f"{host}/"

    now = int(time.time())
    claims = {
        "iss": issuer,
        "sub": args.subject,
        "aud": args.audience,
        "iat": now,
        "nbf": now,
        "exp": now + args.lifetime,
    }

    token = jwt.encode(claims, private_key, algorithm="RS256")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(token)
    os.chmod(args.output, 0o600)

    print(f"issuer   {issuer}")
    print(f"subject  {args.subject}")
    print(f"audience {args.audience}")
    print(f"expires  in {args.lifetime}s")
    print(f"written  {args.output}")

    if args.show:
        print()
        print(token)

    return 0


if __name__ == "__main__":
    sys.exit(main())
