namespace SnapCd.Server.Core.Services.Email.Transport;

public class NoOpEmailTransport : IEmailTransport
{
    public Task<bool> SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
        => Task.FromResult(false);

    public Task<bool> IsDeliveryActiveAsync(CancellationToken ct = default) => Task.FromResult(false);
}
