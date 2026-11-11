// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using SnapCd.Cli.Configuration;

namespace SnapCd.Cli.Auth;

/// <summary>
/// Signs a user in through the browser: authorization code with PKCE, received on a loopback port
/// chosen here (RFC 8252). The Server accepts any port because the client is registered as native.
/// </summary>
public class BrowserLogin
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private readonly OAuthClient _client;
    private readonly TextWriter _out;

    public BrowserLogin(OAuthClient client, TextWriter output)
    {
        _client = client;
        _out = output;
    }

    public async Task<TokenSet> RunAsync(bool openBrowser, CancellationToken ct)
    {
        var port = FreePort();
        var redirectUri = $"http://127.0.0.1:{port}/callback";
        var pkce = Pkce.Create();
        var state = Pkce.Base64Url(RandomNumberGenerator.GetBytes(16));

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/callback/");
        listener.Start();

        var url = _client.AuthorizeUrl(redirectUri, pkce, state);
        _out.WriteLine("Sign in to Snap CD in your browser. If it does not open, go to:");
        _out.WriteLine(url);
        if (openBrowser)
            TryOpen(url);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        HttpListenerContext request;
        try
        {
            request = await listener.GetContextAsync().WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new CliException($"No sign-in arrived within {Timeout.TotalMinutes:0} minutes.");
        }

        var query = HttpUtility.ParseQueryString(request.Request.Url?.Query ?? "");
        var error = query["error"];
        var ok = error is null && query["state"] == state && !string.IsNullOrEmpty(query["code"]);
        await RespondAsync(request.Response, ok
            ? "Signed in to Snap CD. You can close this tab."
            : "Sign-in to Snap CD failed. See the terminal.");

        if (error is not null)
            throw new CliException($"Sign-in failed: {query["error_description"] ?? error}");
        if (query["state"] != state)
            throw new CliException("Sign-in failed: the response did not belong to this login.");
        if (string.IsNullOrEmpty(query["code"]))
            throw new CliException("Sign-in failed: the Server returned no code.");

        return await _client.ExchangeCodeAsync(query["code"]!, redirectUri, pkce, ct);
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static async Task RespondAsync(HttpListenerResponse response, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body);
        response.Close();
    }

    private static void TryOpen(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // The URL is printed above, so the user can open it by hand.
        }
    }
}
