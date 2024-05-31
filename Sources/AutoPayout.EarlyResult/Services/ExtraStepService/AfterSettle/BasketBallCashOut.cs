using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
public class BasketBallCashOut(IOptions<AppSettingsKey> options, IHttpClientFactory httpClientFactory) : IExtraStepService
{
    public async Task Process(Match match, SettleCriteria settleCriteria)
    {
        var listHalf = GetListHalf(match.ResultDetails.Select(r => r.ResourceId));
        var betStatus = match.MatchDetail.GameSession switch
        {
            GameType.Bo2 => BasketBallHelperService.GetBetStatusBoTwo(match),
            GameType.Bo1 => BasketBallHelperService.GetBetStatusBoOne(match),
            _ => settleCriteria.BetStatus
        };
        var criteria = new CashOutCriteria(match.MatchId, betStatus, listHalf.ToArray());
        await PostAsync(criteria);
    }

    private async Task PostAsync(object request)
    {
        try
        {
            var json = JsonConvert.SerializeObject(request);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = httpClientFactory.CreateClient();

            await httpClient.PostAsync(new Uri(options.Value.ThirdPartyUri.JackTimeResult), requestContent);
            await Fanex.Logging.LogService.ErrorAsync($"Auto Payout (Call JackTime API): {options.Value.ThirdPartyUri.JackTimeResult} : {json}");
        }
        catch (Exception exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Auto Payout (Call JackTime API): {options.Value.ThirdPartyUri.JackTimeResult}", exception);
        }
    }

    private static IEnumerable<int> GetListHalf(IEnumerable<string> resourceIds)
    {
        var listHalf = new List<int>();

        var resourceOrdered = resourceIds.OrderBy(r => Array.IndexOf(CashOutConstants.ResourceOrder, r));

        foreach (var resource in resourceOrdered)
        {
            var lsHalf = CashOutConstants.QuarterListHalf.TryGetValue(resource, out var value);
            if (lsHalf)
            {
                listHalf.Add(value);
            }
        }

        return listHalf;
    }
}
