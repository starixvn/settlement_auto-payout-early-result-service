using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
public interface OverTimeService
{
    Task<ResultDetail> GetOtBasketBall(ResultDetail finalResult, int totalHomeValue, int totalAwayValue);
}

public class OverTimeServiceImp(ResultDetailService resultDetailService, MatchDetailService matchDetailService) : OverTimeService
{
    public async Task<ResultDetail> GetOtBasketBall(ResultDetail finalResult, int totalHomeValue, int totalAwayValue)
    {
        var resultOt = new ResultDetail()
        {
            MatchId = finalResult.MatchId,
            SportType = finalResult.SportType,
            ResourceId = ResourceIds.OverTime
        };

        var scoresOt = (await resultDetailService.GetSingleScore(resultOt)).ToList();
        //var settings = await matchDetailService.GetAutoSettings(finalResult.LeagueId, finalResult.SportType);
        //  var primaryProvider = settings.Main_PrimaryProvider.Split(',');
        // var secondaryProvider = settings.Main_SecondaryProvider.Split(',');
        //var scoreMatchWithSetting = scoresOt.Where(s =>
        //primaryProvider.Contains(s.SourceId.ToString()) || secondaryProvider.Contains(s.SourceId.ToString()))
        // .ToList();
        var isUniqueScore = scoresOt.Distinct(new ScoreComparer()).Count() == 1;

        if (isUniqueScore)
        {
            resultOt.HomeValue = scoresOt[0].HomeValue;
            resultOt.AwayValue = scoresOt[0].AwayValue;
            return resultOt;
        }

        return null;
    }
}
