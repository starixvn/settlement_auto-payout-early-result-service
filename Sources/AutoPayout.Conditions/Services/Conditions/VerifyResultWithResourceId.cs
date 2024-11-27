using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyResultWithResourceId : ICondition
{
    private readonly ResultDetailService resultDetailService;
    private readonly IDynamicRepository repository;
    public VerifyResultWithResourceId(ResultDetailService resultDetailService, IDynamicRepository repository)
    {
        this.resultDetailService = resultDetailService;
        this.repository = repository;
    }

    public async Task HandleCondition(Match match)
    {
        foreach (var result in match.ResultDetails)
        {
            var resourceId = result.ResourceId;

            if (!HasSpecialResources(result))
            {
                continue;
            }

            result.IsValid = resourceId switch
            {
                var id when id.StartsWith(BetTypes.SetXWinner) => VerifyScoreRaceToPoint(result),
                var id when id.StartsWith(BetTypes.SetXRaceToPoints) => VerifyScorePointsPlayed(result),
                _ => result.IsValid
            };
        }


        await HandleInvalidResults(match);

        match.ResultDetails = match.ResultDetails.Where(x => x.IsValid).ToList();

        if (match.ResultDetails.Count == 0)
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private async Task HandleInvalidResults(Match match)
    {
        var invalidResults = match.ResultDetails
            .Where(x => !x.IsValid)
            .ToList();

        if (!invalidResults.Any())
        {
            return;
        }

        var errorMessage = string.Format(ErrorMessage.InvalidInput, string.Join(",", invalidResults.Select(x => x.ToString())));
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, invalidResults, errorMessage);
        await repository.ExecuteAsync(new WriteLogCriteria(match.MatchId, errorMessage));
    }

    private bool HasSpecialResources(ResultDetail item)
        => item.Scores.Count != 0 && (item.ResourceId.StartsWith(BetTypes.SetXWinner) || item.ResourceId.StartsWith(BetTypes.SetXRaceToPoints));

    private static bool VerifyScoreRaceToPoint(ResultDetail result)
    {
        if (!TryParseScores(result, out var homeScore, out var awayScore))
        {
            return VerifyValuesAreNone(result.HomeValue, result.AwayValue);
        }

        var score = ParseResourceScore(result.ResourceId);
        return (score == homeScore && awayScore < score) || (score == awayScore && homeScore < score);
    }

    private static bool VerifyScorePointsPlayed(ResultDetail result)
    {
        if (!TryParseScores(result, out var homeScore, out var awayScore))
        {
            return VerifyValuesAreNone(result.HomeValue, result.AwayValue);
        }

        var score = int.Parse(result.ResourceId[^2..]);
        return score == (homeScore + awayScore);
    }

    private static bool TryParseScores(ResultDetail result, out int homeScore, out int awayScore)
    {
        homeScore = 0;
        awayScore = 0;
        return int.TryParse(result.HomeValue, out homeScore) && int.TryParse(result.AwayValue, out awayScore);
    }

    private static int ParseResourceScore(string resourceId) => int.Parse(resourceId[^2..]);

    private static bool VerifyValuesAreNone(string homeValue, string awayValue)
        => homeValue.Equals("N", StringComparison.Ordinal) && awayValue.Equals("N", StringComparison.Ordinal);

}
