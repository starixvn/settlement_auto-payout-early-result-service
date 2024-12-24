using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;

public interface ExtraResultService
{
    Task<ResultDetail> GetOtBasketBall(ResultDetail finalResult, int totalHomeValue, int totalAwayValue);

    Task<ResultDetail> GetScoreExtraInning(Match match, int totalHomeValue, int totalAwayValue);
}

public class OverTimeServiceImp(ResultDetailService resultDetailService, IOptions<AppSettingsKey> appSettingsKey) : ExtraResultService
{
    private const string overTimeDefaultValue = "0";

    public async Task<ResultDetail> GetOtBasketBall(ResultDetail finalResult, int totalHomeValue, int totalAwayValue)
    {
        var resultOt = new ResultDetail
        {
            MatchId = finalResult.MatchId,
            SportType = finalResult.SportType,
            ResourceId = ResourceIds.OverTime,
            HomeValue = overTimeDefaultValue,
            AwayValue = overTimeDefaultValue
        };

        var scoresOt = (await resultDetailService.GetSingleScore(resultOt)).ToList();

        var scoresValid = scoresOt.Where(IsValidNonZeroScore).ToList();

        resultOt.IsValid = IsOtValid(resultOt, totalHomeValue, totalAwayValue, scoresValid);
        resultOt.RelatedInfo = GetRelatedInfo(scoresValid);

        if (resultOt.IsValid && totalAwayValue == totalHomeValue)
        {
            resultOt.HomeValue = scoresValid[0].HomeValue;
            resultOt.AwayValue = scoresValid[0].AwayValue;
        }

        return resultOt;
    }

    public async Task<ResultDetail> GetScoreExtraInning(Match match, int totalHomeValue, int totalAwayValue)
    {
        var result = new ResultDetail
        {
            MatchId = match.MatchId,
            SportType = match.SportType,
            ResourceId = ResourceIds.ExtraInning,
            HomeValue = "",
            AwayValue = ""
        };

        var validScores = (await resultDetailService.GetSingleScore(result))
            .Where(IsValidScore)
            .ToList();

        result.IsValid = IsExtraScoreValid(result, totalHomeValue, totalAwayValue, validScores);
        result.RelatedInfo = GetRelatedInfo(validScores);

        if (!result.IsValid)
        {
            return result;
        }

        if (IsTieGame(totalHomeValue, totalAwayValue))
        {
            AssignFirstValidScore(result, validScores);
        }
        else if (IsZeroScore(validScores))  //NOSONAR
        {
            ClearScore(result);
        }

        return result;
    }

    private bool IsOtValid(ResultDetail resultOt, int totalHomeValue, int totalAwayValue, IReadOnlyList<Score> scores)
    {
        var variantCount = scores.Distinct(new ScoreComparer()).Count();

        if (totalAwayValue != totalHomeValue)
        {
            return variantCount == 0;
        }

        return variantCount switch
        {
            > 1 => HandleMultipleVariants(resultOt, scores),
            1 => AreScoresUnEqual(scores),
            _ => false
        };
    }

    private bool HandleMultipleVariants(ResultDetail resultOt, IEnumerable<Score> scores)
    {
        var maxDate = scores.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;

        resultOt.Expired = processTime.TotalMilliseconds > appSettingsKey.Value.RetryProcessScore;
        resultOt.NeedRetry = !resultOt.Expired;

        return false;
    }

    private static bool AreScoresUnEqual(IReadOnlyList<Score> scores) => int.Parse(scores[0].HomeValue) != int.Parse(scores[0].AwayValue);

    private bool IsExtraScoreValid(ResultDetail resultExtra, int totalHomeValue, int totalAwayValue, IReadOnlyList<Score> scores)
    {
        var variantCount = scores.Distinct(new ScoreComparer()).Count();

        if (totalAwayValue != totalHomeValue)
        {
            return variantCount == 0 || variantCount == 1 && IsValidSpecialExtraScore(scores[0]);
        }

        return variantCount <= 1 || HandleMultipleVariants(resultExtra, scores);
    }

    private static bool IsValidSpecialExtraScore(Score score) => (score.HomeValue == "0" && score.AwayValue == "0")
        || (string.IsNullOrEmpty(score.AwayValue) && string.IsNullOrEmpty(score.HomeValue));

    private static bool IsValidScore(Score score) => !string.IsNullOrEmpty(score.HomeValue) && !string.IsNullOrEmpty(score.AwayValue);

    private static bool IsValidNonZeroScore(Score score)
        => !string.IsNullOrEmpty(score.HomeValue)
        && !string.IsNullOrEmpty(score.AwayValue)
        && (score.HomeValue != "0" || score.AwayValue != "0");

    private static string GetRelatedInfo(IEnumerable<Score> validScores)
        => string.Join(',', validScores.Select(score => $"{score.SourceName ?? "Trader"}: {score.HomeValue}-{score.AwayValue}"));

    private static bool IsTieGame(int totalHomeValue, int totalAwayValue) => totalHomeValue == totalAwayValue;

    private static void AssignFirstValidScore(ResultDetail result, IReadOnlyList<Score> validScores)
    {
        if (validScores.Any())
        {
            result.HomeValue = validScores[0].HomeValue;
            result.AwayValue = validScores[0].AwayValue;
        }
    }

    private static void ClearScore(ResultDetail result)
    {
        result.HomeValue = string.Empty;
        result.AwayValue = string.Empty;
    }

    private static bool IsZeroScore(IReadOnlyList<Score> validScores)
        => validScores.Count >= 1
        && validScores[0].HomeValue == "0"
        && validScores[0].AwayValue == "0";
}