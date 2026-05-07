namespace SnapCd.Server.Core.Services.Email.Transport;

public interface IEmailTransport
{
    /// <summary>
    /// Sends the email and returns true if it was actually delivered, false if it was no-op'd
    /// (either because this transport is the no-op transport, or because a routing decorator
    /// — the licence gate — downgraded the call to no-op). Throws on transport failure.
    /// </summary>
    Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);

    /// <summary>
    /// Predictive: returns true if a <see cref="SendAsync"/> call right now would actually
    /// deliver. Use for UI decisions made before any send. After a send, branch on the bool
    /// returned by <see cref="SendAsync"/> instead — that is authoritative.
    /// </summary>
    Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default) => Task.FromResult(true);
}
