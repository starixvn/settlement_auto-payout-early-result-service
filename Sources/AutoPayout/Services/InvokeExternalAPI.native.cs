using Newtonsoft.Json;
using System.Text;

namespace AutoPayout.Services;
public interface InvokeExternalAPI
{
    Task PostAsync(object request, string url);
}

public class InvokeExternalAPIImp(IHttpClientFactory httpClientFactory) : InvokeExternalAPI
{
    public async Task PostAsync(object request, string url)
    {
        var json = JsonConvert.SerializeObject(request);
        try
        {
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = httpClientFactory.CreateClient();

            await httpClient.PostAsync(new Uri(url), requestContent);
            await Fanex.Logging.LogService.InfoAsync($"Auto Payout (Call API): {url} : {json}");
        }
        catch (Exception exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Auto Payout (Call API Failed): {url} : {json}", exception);
        }
    }
}
