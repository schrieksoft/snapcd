using System.Net;

namespace SnapCd.Runner.Exceptions;

public class HttpClientException : Exception
{
    public HttpClientException(HttpStatusCode status, string message)
        : base($"{status}: {message}")
    {
    }
}