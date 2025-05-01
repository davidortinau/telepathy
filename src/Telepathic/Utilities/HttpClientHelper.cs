namespace Telepathic.Utilities;

/// <summary>
/// Helper class to manage HttpClient configuration and Url endpoint addresses.
/// </summary>
internal class HttpClientHelper
{
    private static string _baseUrl = "https://localhost:7228/";
    public static string BaseUrl
    {
        get
        {
#if DEBUG
            //See: https://learn.microsoft.com/dotnet/maui/data-cloud/local-web-services
            //Android Emulator uses 10.0.2.2 to refer to localhost
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                _baseUrl = _baseUrl.Replace("localhost", "10.0.2.2");
            }
#endif
            return _baseUrl;
        }
    }
    public static string TeamTaskLoadUrl => $"{BaseUrl}api/teamtaskload";
    public static string CategoryTaskLoadUrl => $"{BaseUrl}api/categorytaskload";
    public static string TasksDueUrl => $"{BaseUrl}api/tasksdue";

    public static HttpClient GetHttpClient()
    {
#if WINDOWS || MACCATALYST
        return new HttpClient();
#else
        return new HttpClient(new HttpsClientHandlerService().GetPlatformMessageHandler());
#endif
    }
}

internal class HttpsClientHandlerService
{
    public HttpMessageHandler GetPlatformMessageHandler()
    {
#if ANDROID
        var handler = new Xamarin.Android.Net.AndroidMessageHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert != null && cert.Issuer.Equals("CN=localhost"))
                return true;
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
#elif IOS
        var handler = new NSUrlSessionHandler
        {
            TrustOverrideForUrl = IsHttpsLocalhost
        };
        return handler;
#else
        throw new PlatformNotSupportedException("Only Android and iOS supported.");
#endif
    }

#if IOS
    public bool IsHttpsLocalhost(NSUrlSessionHandler sender, string url, Security.SecTrust trust)
    {
        return url.StartsWith("https://localhost");
    }
#endif
}
