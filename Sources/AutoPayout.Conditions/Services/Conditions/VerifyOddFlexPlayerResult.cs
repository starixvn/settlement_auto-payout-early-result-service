using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using System.Globalization;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyOddFlexPlayerResult(ResultDetailService resultDetailService, IDynamicRepository repository) : AbstractCondition
{
    private static List<int> PlayerBetTypes => [653, 654, 655, 685, 656, 657, 658, 686, 659, 660, 661, 687, 662, 663, 664, 688,
        665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 678, 677, 679, 680, 681, 682, 683, 684];


    public async override Task HandleCondition(Match match)
    {
        var playerResults = match.ResultDetails
            .Where(x => PlayerBetTypes.Exists(p => x.ResourceId.StartsWith(p.ToString(CultureInfo.InvariantCulture))))
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

        match.ResultDetails = [.. nonPlayerResults, .. playerResults.Where(x => x.IsValid)];

        await HandleNextCondition(match);
    }

    private async Task ValidatePlayerResults(Match match, List<ResultDetail> playerResults)
    {
        var playerInfo = await GetOddFlex(match.MatchId, [.. playerResults.Select(x => x.ResourceId)]);
        foreach (var result in playerResults)
        {
            var confirmScore = result.Scores.FirstOrDefault(s => s.SourceId == 0);
            var playerOtherInfo = GetOrtherInfoByOddFlex(result, playerInfo);

            result.IsValid = confirmScore == null ||
                string.IsNullOrEmpty(confirmScore.OtherInfo) ||
                confirmScore.OtherInfo.Equals(playerOtherInfo);

            if (result.IsValid)
            {
                result.OtherInfo = playerOtherInfo;
            }
        }

        var invalidResults = playerResults.Where(x => !x.IsValid).ToList();
        await ProcessFailureResults(match, invalidResults);
    }

    private async Task<IEnumerable<OddFlexModel>> GetOddFlex(int matchId, List<string> resourceIds)
    {
        var criteria = new GetPlayerInfoCriteria
        {
            MatchId = matchId,
            SportType = SportType.BasketBall,
            ListResourceIds = string.Join(",", resourceIds)
        };

        var oddFlex = (await repository.FetchAsync<OddFlexModel>(criteria))
            .Distinct()
            .ToList();

        return oddFlex;
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

        return string.Join(",", oddFlexs.Select(x => $"{x.Info.PlayerId},${x.Info.TeamId}"));
    }

    private async Task ProcessFailureResults(Match match, List<ResultDetail> failureResults)
    {
        if (failureResults.Count == 0)
        {
            return;
        }

        const string remark = "Score mismatch with OddFlex data";
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, failureResults, remark);
    }
}
