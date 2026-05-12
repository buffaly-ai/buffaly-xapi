using System.ComponentModel;
using System.Text;

namespace XApiClient.CLI.Output;

public static class HttpRequestErrorFormatter
{
    private const int SecENoCredentials = unchecked((int)0x8009030E);

    public static string Format(HttpRequestException exception)
    {
        StringBuilder message = new StringBuilder();
        message.Append(exception.Message);

        Exception? inner = exception.InnerException;
        while (inner != null)
        {
            if (!string.IsNullOrWhiteSpace(inner.Message))
            {
                message.Append(" Inner: ");
                message.Append(inner.Message);
            }

            if (inner is Win32Exception win32Exception &&
                win32Exception.NativeErrorCode == SecENoCredentials)
            {
                message.Append(" TLS setup failed on this machine (SEC_E_NO_CREDENTIALS).");
                message.Append(" Check Windows Schannel/proxy/client-certificate configuration.");
                break;
            }

            inner = inner.InnerException;
        }

        return message.ToString();
    }
}
