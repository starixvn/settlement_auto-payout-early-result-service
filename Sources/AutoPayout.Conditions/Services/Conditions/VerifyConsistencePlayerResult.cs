using AutoPayout.Conditions.Services.HelperService;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using System.Globalization;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyConsistencePlayerResult(ResultDetailService resultDetailService) : AbstractCondition
{
    public async override Task HandleCondition(Match match)
    {
        var playerResults = match.ResultDetails
            .Where(x => BetTypes.Player.Exists(p => x.ResourceId.StartsWith(p.ToString(CultureInfo.InvariantCulture))))
            .ToList();

        if (playerResults.Count == 0)
        {
            await HandleNextCondition(match);
            return;
        }

        var nonPlayerResults = match.ResultDetails
            .Except(playerResults)
            .ToList();

        await ValidatePlayerResults(match, playerResults);

        var failedResults = playerResults.Where(x => !x.IsValid).ToList();

        await ProcessFailureResults(match, failedResults);

        match.ResultDetails = [.. nonPlayerResults, .. playerResults.Where(x => x.IsValid)];

        await HandleNextCondition(match);
    }

    private async Task ValidatePlayerResults(Match match, List<ResultDetail> playerResults)
    {
        foreach (var result in playerResults)
        {
            var score = result.Scores.FirstOrDefault(x => x.SourceId == 0);
            var playerOtherInfo = GetOrtherInfoByOddFlex(result, result.OddFlex);

            var isValidOtherInfo = score == null ||
                string.IsNullOrEmpty(score.OtherInfo) ||
                score.OtherInfo.Equals(playerOtherInfo);

            if (isValidOtherInfo)
            {
                result.OtherInfo = playerOtherInfo;
            }

            var isValidConsistent = result.ValidateByConsistenceRule();

            result.IsValid = isValidOtherInfo && isValidConsistent;
        }

        var invalidResults = playerResults.Where(x => !x.IsValid).ToList();
        await ProcessFailureResults(match, invalidResults);
    }

    private static string GetOrtherInfoByOddFlex(ResultDetail result, IEnumerable<OddFlexModel> oddFlexs)
    {
        var relevantOddFlexModels = oddFlexs
            .Where(p => p.ResourceId == result.ResourceId)
            .ToList();

        if (relevantOddFlexModels.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(",", oddFlexs.Select(x => $"{x.Info.PlayerId},{x.Info.TeamId}"));
    }

    private async Task ProcessFailureResults(Match match, List<ResultDetail> failureResults)
    {
        if (failureResults.Count == 0)
        {
            return;
        }

        var remark = $"Failed Auto Payout: Selected({string.Join(", ", failureResults.Select(x => x.ResourceId))}) due to base result values inconsistency";
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, failureResults, remark);
    }
}
