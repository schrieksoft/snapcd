# Security Policy

Thank you for helping keep Snap CD and its users safe. We take security seriously and appreciate responsible disclosure.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

Use one of these private channels instead:

- **GitHub private advisory** *(preferred)* — open a report via the repository's **Security → Report a vulnerability** tab ([how to](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability)).
- **Email** — [security@snapcd.io](mailto:security@snapcd.io). Send an initial low-detail message first if you would like to exchange a key and encrypt the full report.

Where possible, please include:

- a description of the issue and its impact;
- the affected component(s) and version(s) — e.g. Server or Runner;
- step-by-step reproduction or a proof of concept;
- any suggested remediation.

## What to expect

- We aim to **acknowledge your report within 3 business days**.
- We will keep you updated as we investigate, work on a fix, and prepare a release.
- With your permission, we will **credit you** in the published advisory; let us know if you prefer to remain anonymous.

## Supported versions

Security fixes are released against the **latest published version** of Snap CD. We strongly recommend always running the most recent release.

| Version | Supported |
|---------|-----------|
| Latest release | ✅ |
| Older releases | ❌ — upgrade to receive fixes |

## Scope

This policy covers the Snap CD application in this repository (Server, Runner, Contracts/Core). Components that ship from other repositories (such as the Terraform provider) are covered by the security policy in their own repository.

Generally **out of scope** (please report to the relevant party instead):

- Vulnerabilities in third-party dependencies — report them upstream; we will update once a fix is available.
- Findings that require a compromised host, physical access, or a deliberately insecure non-default configuration.

Reports about the hosted Cloud service at [snapcd.io](https://snapcd.io) are also welcome at the address above.

## Safe harbor

We will not pursue or support legal action against researchers who:

- make a good-faith effort to follow this policy;
- avoid privacy violations, data destruction, and degradation of service; and
- give us reasonable time to remediate before any public disclosure.
