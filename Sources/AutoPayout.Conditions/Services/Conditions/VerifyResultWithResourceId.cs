using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyResultWithResourceId : ICondition
{
    private readonly AppSettingsKey appSettings;
    private readonly ResultDetailService resultDetailService;
    private readonly IDynamicRepository repository;
    public VerifyResultWithResourceId(IOptions<AppSettingsKey> options, ResultDetailService resultDetailService, IDynamicRepository repository)
    {
        appSettings = options.Value;
        this.resultDetailService = resultDetailService;
        this.repository = repository;
    }

    public async Task HandleCondition(Match match)
    {
        foreach (var result in match.ResultDetails)
        {
            var resourceId = result.ResourceId;

            if (!HasSpecialResources(result)) { continue; }

            result.IsValid = resourceId switch
            {
                var id when id.StartsWith(BetTypes.SetXWinner) => VerifyScoreRaceToPoint(result),
                var id when id.StartsWith(BetTypes.SetXRaceToPoints) => VerifyScorePointsPlayed(result),
                _ => result.IsValid
            };

            SetExpiredResult(result);
        }


        await HandleExpiredResults(match);

        match.ResultDetails = match.ResultDetails.Where(x => x.IsValid).ToList();
    }

    private async Task HandleExpiredResults(Match match)
    {
        var expiredResults = match.ResultDetails
            .Where(x => !x.IsValid && x.Expired)
            .ToList();

        if (!expiredResults.Any()) { return; }

        var errorMessage = string.Format(ErrorMessage.InvalidInput, string.Join(",", expiredResults.Select(x => x.ToString())));
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, expiredResults, errorMessage);
        await repository.ExecuteAsync(new WriteLogCriteria(match.MatchId, errorMessage));
    }

    private bool HasSpecialResources(ResultDetail item)
        => item.Scores.Count != 0 && (item.ResourceId.StartsWith(BetTypes.SetXWinner) || item.ResourceId.StartsWith(BetTypes.SetXRaceToPoints));

    private void SetExpiredResult(ResultDetail resultDetail)
    {
        var maxDate = resultDetail.Scores.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;

        resultDetail.Expired = processTime.TotalMilliseconds > appSettings.RetryProcessScore;
    }

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
