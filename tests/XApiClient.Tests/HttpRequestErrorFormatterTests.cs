using System.ComponentModel;
using System.Security.Authentication;
using XApiClient.CLI.Output;

namespace XApiClient.Tests;

public sealed class HttpRequestErrorFormatterTests
{
    [Fact]
    public void Format_ShouldIncludeInnerExceptionMessages_WhenNestedExceptionsExist()
    {
        // Ensures transport failures preserve nested context so users can diagnose TLS problems quickly.
        InvalidOperationException inner = new InvalidOperationException("low-level detail");
        AuthenticationException auth = new AuthenticationException("auth handshake failed", inner);
        HttpRequestException http = new HttpRequestException("request failed", auth);

        string message = HttpRequestErrorFormatter.Format(http);

        Assert.Contains("request failed", message, StringComparison.Ordinal);
        Assert.Contains("auth handshake failed", message, StringComparison.Ordinal);
        Assert.Contains("low-level detail", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ShouldAppendTlsGuidance_WhenSecENoCredentialsIsDetected()
    {
        // Verifies Schannel credential errors are mapped to an actionable machine-level TLS hint.
        Win32Exception schannel = new Win32Exception(unchecked((int)0x8009030E));
        AuthenticationException auth = new AuthenticationException("authentication failed", schannel);
        HttpRequestException http = new HttpRequestException("ssl failed", auth);

        string message = HttpRequestErrorFormatter.Format(http);

        Assert.Contains("SEC_E_NO_CREDENTIALS", message, StringComparison.Ordinal);
        Assert.Contains("Schannel/proxy/client-certificate", message, StringComparison.Ordinal);
    }
}
