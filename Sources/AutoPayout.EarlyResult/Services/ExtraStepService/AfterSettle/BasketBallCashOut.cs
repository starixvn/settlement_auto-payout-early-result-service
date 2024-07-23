using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;
using AutoPayout.Services;
using Microsoft.Extensions.Options;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
public class BasketBallCashOut(IOptions<AppSettingsKey> options, InvokeExternalAPI invokeExternalApi) : IExtraStepService
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
        await invokeExternalApi.PostAsync(criteria, options.Value.ThirdPartyUri.JackTimeResult);
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
