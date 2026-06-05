// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Logging;

namespace SnapCd.Server.Core.Tests.Tests.Logging;

public class DefaultLogRedactorTests
{
    private readonly DefaultLogRedactor _redactor = new();

    [Theory]
    [InlineData("My AWS key is AKIAIOSFODNN7EXAMPLE today", "[REDACTED:aws_access_key_id]")]
    [InlineData("aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", "[REDACTED:aws_secret_access_key]")]
    [InlineData("token: sk_live_4eC39HqLyjWDarjtT1zdp7dc more text", "[REDACTED:stripe_live]")]
    [InlineData("github token ghp_abcdefghijklmnopqrstuvwxyz0123456789", "[REDACTED:github_pat]")]
    [InlineData("auth header: Bearer abc.def.ghi.jkl.mno.pqr.stu.vwx.yz0.123.456.789",
        "[REDACTED:bearer_header]")]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
        "[REDACTED:bearer_header]")]
    [InlineData("password=hunter2 in config", "[REDACTED:password_form]")]
    [InlineData("apikey: super_secret_value!", "[REDACTED:password_form]")]
    public void Redact_replaces_known_credential_patterns(string raw, string expectedSubstring)
    {
        var result = _redactor.Redact(raw);
        Assert.Contains(expectedSubstring, result);
    }

    [Theory]
    [InlineData("Plain log message with no secrets")]
    [InlineData("Stack trace at System.Foo.Bar() at line 42")]
    [InlineData("https://api.example.com/v1/users — request succeeded")]
    [InlineData("module-id: a3f2e8b1-4c7d-49a0-b8e5-2f4c3d1a9b6e (regular GUID)")]
    public void Redact_leaves_benign_content_untouched(string raw)
    {
        var result = _redactor.Redact(raw);
        Assert.Equal(raw, result);
    }

    [Fact]
    public void Redact_handles_empty_input()
    {
        Assert.Equal(string.Empty, _redactor.Redact(string.Empty));
    }

    [Fact]
    public void Redact_handles_null_safely()
    {
        Assert.Null(_redactor.Redact(null!));
    }

    [Fact]
    public void RedactWithRetention_flags_unacceptable_when_input_is_mostly_credentials()
    {
        // 6 AWS keys back-to-back, almost no surrounding context.
        var raw = string.Join(" ", Enumerable.Repeat("AKIAIOSFODNN7EXAMPLE", 6));
        var result = _redactor.RedactWithRetention(raw, minRetentionRatio: 0.5);

        Assert.False(result.Acceptable);
    }

    [Fact]
    public void RedactWithRetention_accepts_when_credentials_are_a_small_fraction()
    {
        var raw = "Long log message of mostly innocuous content with one secret token=foo at the end and lots of plain text after.";
        var result = _redactor.RedactWithRetention(raw, minRetentionRatio: 0.5);

        Assert.True(result.Acceptable);
    }

    [Fact]
    public void RedactWithRetention_validates_threshold_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _redactor.RedactWithRetention("x", -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _redactor.RedactWithRetention("x", 1.1));
    }
}
