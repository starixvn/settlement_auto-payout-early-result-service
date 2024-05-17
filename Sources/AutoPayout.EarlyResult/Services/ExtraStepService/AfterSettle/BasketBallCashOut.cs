using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
public class BasketBallCashOut(IOptions<AppSettingsKey> options, IHttpClientFactory httpClientFactory) : IExtraStepService
{
    private const string DefaultBetStatus = "0-0,0-0,0-0,0-0,0-0";

    private const byte HtPosition = 0;

    private const byte FtPosition = 2;

    public async Task Process(Match match, SettleCriteria settleCriteria)
    {
        var listHalf = GetListHalf(match.ResultDetails.Select(r => r.ResourceId));
        var betStatus = match.MatchDetail.GameSession switch
        {
            GameType.Bo4 => GetBetStatusBoFour(match),
            GameType.Bo2 => GetBetStatusBoTwo(match),
            _ => GetBetStatusBoOne(match)
        };
        var criteria = new CashOutCriteria(match.MatchId, betStatus, listHalf.ToArray());
        await PostAsync(criteria);
    }

    private async Task<HttpResponseMessage> PostAsync(object request)
    {
        try
        {
            var requestContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var httpClient = httpClientFactory.CreateClient();
            return await httpClient.PostAsync(new Uri(options.Value.ThirdPartyUri.JackTimeResult), requestContent);
        }
        catch (Exception exception)
        {
            await Fanex.Logging.LogService.ErrorAsync($"Settlement.WebSites (Call JackTime API): {options.Value.ThirdPartyUri.JackTimeResult}", exception);
            return new HttpResponseMessage();
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

    private string GetBetStatusBoOne(Match match)
    {
        var elements = DefaultBetStatus.Split(",");
        var final = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime);
        elements[FtPosition] = final == null ? "0-0" : $"{final.HomeValue}-{final.AwayValue}";
        return string.Join(",", elements);
    }

    private string GetBetStatusBoTwo(Match match)
    {
        var elements = InitializeElements(AnalyzeBetStatus(match));

        foreach (var resultDetail in match.ResultDetails)
        {
            if (resultDetail.ResourceId != ResourceIds.FullTime && resultDetail.ResourceId != ResourceIds.Halftime)
            {
                continue;
            }
            var position = resultDetail.ResourceId == ResourceIds.FullTime ? FtPosition : HtPosition;
            elements[position] = $"{resultDetail.HomeValue}-{resultDetail.AwayValue}";
        }

        return string.Join(",", elements);
    }


    private string GetBetStatusBoFour(Match match)
    {
        var elements = AnalyzeBetStatus(match);

        foreach (var resultDetail in match.ResultDetails)
        {
            var position = Array.IndexOf(CashOutConstants.ResourceWithOutFinalOrder, resultDetail.ResourceId);
            elements[position] = $"{resultDetail.HomeValue}-{resultDetail.AwayValue}";
        }

        return string.Join(",", elements);
    }

    private List<string> AnalyzeBetStatus(Match match)
    {
        var bestStatus = string.IsNullOrEmpty(match.MatchDetail.BetStatus) ? DefaultBetStatus : match.MatchDetail.BetStatus;
        var elements = GetElements(bestStatus).Split(",").ToList();

        while (elements.Count < DefaultBetStatus.Length)
        {
            elements.Add("0-0");
        }

        return elements;
    }

    private static string GetElements(string betStatus)
    {
        const string pattern = @"\d+-\d+";

        var matches = Regex.Matches(betStatus, pattern);

        var result = string.Join(",", matches);

        return result;
    }

    private List<string> InitializeElements(List<string> elements)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            if (i == HtPosition || i == FtPosition)
            {
                continue;
            }

            elements[i] = "0-0";
        }

        return elements;
    }
}
