using AutoPayout.Conditions.Services.HelperService;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using System.Globalization;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyOddFlexPlayerResult(ResultDetailService resultDetailService) : AbstractCondition
{
    public static List<string> Player => ["653", "654", "655", "685", "656", "657", "658", "686", "659", "660", "661", "687", "662", "663", "664", "688", "665", "666"
        , "667", "668", "669", "670", "671", "672", "673", "674", "675", "676", "678", "677", "679", "680", "681", "682", "683", "684"];


    public async override Task HandleCondition(Match match)
    {
        var playerResults = match.ResultDetails
            .Where(x => Player.Exists(p => x.ResourceId.StartsWith(p.ToString(CultureInfo.InvariantCulture))))
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

            var isValidConsistent = result.ValidateByRule();

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
