using AutoPayout.EarlyResult.Constants;
using System.Text.RegularExpressions;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;

public static class BasketBallHelperService
{
    public const string DefaultBetStatus = "0-0,0-0,0-0,0-0,0-0";

    private const byte HtPosition = 0;

    private const byte FtPosition = 2;

    public static byte GenerateDeterminedType(Match match)
    {
        const int roundBitStartAt = 3;

        var determinedType = match.ResultDetails.Select(resultDetail => resultDetail.ResourceId switch
            {
                ResourceIds.Halftime => 0,
                ResourceIds.FullTime => 2,
                _ => Array.FindIndex(ResourceIds.ResultBasketBall, r => r == resultDetail.ResourceId) + roundBitStartAt
            })
            .Select(bitPosition => Math.Pow(2, bitPosition))
            .Sum();

        return (byte)determinedType;
    }

    public static string GetBetStatusBoFour(Match match)
    {
        var elements = AnalyzeBetStatus(match);

        foreach (var resultDetail in match.ResultDetails)
        {
            var position = Array.IndexOf(CashOutConstants.ResourceWithOutFinalOrder, resultDetail.ResourceId);
            if (position >= 0)
            {
                elements[position] = $"{resultDetail.HomeValue}-{resultDetail.AwayValue}";
            }
        }

        return string.Join(",", elements);
    }

    public static string GetBetStatusBoTwo(Match match)
    {
        var elements = DefaultBetStatus.Split(',');

        foreach (var resultDetail in match.ResultDetails)
        {
            if (resultDetail.ResourceId != ResourceIds.FullTime && resultDetail.ResourceId != ResourceIds.Halftime)
            {
                continue;
            }

            if (resultDetail.ResourceId == ResourceIds.FullTime)
            {
                elements[FtPosition] = $"{resultDetail.HomeValue}-{resultDetail.AwayValue}";
                elements[HtPosition] = resultDetail.RelatedInfo;
            }
            else
            {
                elements[HtPosition] = $"{resultDetail.HomeValue}-{resultDetail.AwayValue}";
            }
        }

        return string.Join(",", elements);
    }

    public static string GetBetStatusBoOne(Match match)
    {
        var elements = DefaultBetStatus.Split(",");
        var final = match.ResultDetails.Find(r => r.ResourceId == ResourceIds.FullTime);
        elements[FtPosition] = final == null ? "0-0" : $"{final.HomeValue}-{final.AwayValue}";
        return string.Join(",", elements);
    }

    private static List<string> AnalyzeBetStatus(Match match)
    {
        var bestStatus = string.IsNullOrEmpty(match.MatchDetail.BetStatus) ? DefaultBetStatus : match.MatchDetail.BetStatus;
        var elements = GetElements(bestStatus).Split(",").ToList();

        while (elements.Count < DefaultBetStatus.Split(",").Length)
        {
            elements.Add("0-0");
        }

        return elements;
    }

    private static string GetElements(string betStatus)
    {
        const string pattern = @"\d+-\d+";
        const int timeoutMilliseconds = 3000;
        try
        {
            var matches = Regex.Matches(betStatus, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(timeoutMilliseconds));

            var result = string.Join(",", matches);

            return result;
        }
        catch (RegexMatchTimeoutException)
        {
            return string.Empty;
        }
    }
}