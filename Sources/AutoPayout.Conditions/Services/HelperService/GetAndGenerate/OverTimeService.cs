using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;

public interface OverTimeService
{
    Task<ResultDetail> GetOtBasketBall(ResultDetail finalResult, int totalHomeValue, int totalAwayValue);
}

public class OverTimeServiceImp(ResultDetailService resultDetailService, IOptions<AppSettingsKey> appSettingsKey) : OverTimeService
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

        var scoresValid = scoresOt.Where(s => !string.IsNullOrEmpty(s.HomeValue) && !string.IsNullOrEmpty(s.AwayValue)
                                                        && (s.HomeValue != "0" || s.AwayValue != "0"))
                                                        .ToList();

        resultOt.IsValid = IsOtValid(resultOt, totalHomeValue, totalAwayValue, scoresValid);
        resultOt.RelatedInfo = string.Join(',', scoresValid.Select(s => $"{s.SourceName ?? "Trader"}: {s.HomeValue}-{s.AwayValue}"));
        if (resultOt.IsValid && totalAwayValue == totalHomeValue)
        {
            resultOt.HomeValue = scoresValid[0].HomeValue;
            resultOt.AwayValue = scoresValid[0].AwayValue;
        }

        return resultOt;
    }

    private bool IsOtValid(ResultDetail resultOt, int totalHomeValue, int totalAwayValue, IReadOnlyList<Score> scores)
    {
        var variantCount = scores.Distinct(new ScoreComparer()).Count();
        if (totalAwayValue != totalHomeValue)
        {
            return variantCount == 0;
        }

        switch (variantCount)
        {
            case > 1:
                {
                    var maxDate = scores.Max(t => t.CreateDate);
                    var processTime = DateTimeOffset.Now - maxDate;
                    resultOt.Expired = processTime.TotalMilliseconds > appSettingsKey.Value.RetryProcessScore;
                    resultOt.NeedRetry = !resultOt.Expired;
                    return false;
                }
            case 1:
                return int.Parse(scores[0].HomeValue) != int.Parse(scores[0].AwayValue);

            default:
                return false;
        }
    }
}