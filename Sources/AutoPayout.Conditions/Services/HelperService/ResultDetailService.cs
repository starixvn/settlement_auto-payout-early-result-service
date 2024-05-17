using AutoPayout.Constants;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.Conditions.Services.HelperSerivce;

public interface ResultDetailService
{
    Task<IEnumerable<Score>> GetScore(Match match);

    string WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores);

    string WriteLogResultNotTrader(int matchId, ResultDetail result);

    void WriteLogResultNotTrader(int matchId, ResultDetail result, List<Score> scores);

    void WriteLogResult(int matchId, ResultDetail result, string message);

    Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails, string remark = "");

    Task<(List<ResultDetail>, List<ResourceIndex>)> GetResultsChecking(Match match, int maxResultIndex, List<ResultDetail> resultSettled);

    Task<List<ResultDetail>> GetResultSettled(int matchId, int sportType);
}

public class ResultDetailServiceImp(IDynamicRepository repository, WriteLogService writeLogService,
        UpdateResultStatusService updateResultStatusService)
    : ResultDetailService
{
    public async Task<IEnumerable<Score>> GetScore(Match match)
    {
        var resources = match.ResultDetails.Select(r => r.ResourceId);
        var criteria = new GetConfirmScoreCriteria
        {
            MatchId = match.MatchId,
            SportType = match.SportType,
            ResourceIds = string.Join(',', resources)
        };

        var scores = (await repository.FetchAsync<Score>(criteria)).ToList();

        return scores;
    }

    public string WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, result.ResourceId);
        var traderScore = TraderScore(scores);
        var sourceScore = ProviderScore(scores);

        if (!string.IsNullOrEmpty(traderScore))
        {
            sourceScore.Insert(0, traderScore);
        }

        var message = errorMessage + string.Join(", ", sourceScore);
        writeLogService.WriteLog(cloneMatch, errorMessage + string.Join(", ", sourceScore));

        return message;
    }

    public string WriteLogResultNotTrader(int matchId, ResultDetail result)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var message = string.Format(ErrorMessage.NotTrader, result.ResourceId);

        writeLogService.WriteLog(cloneMatch, message);

        return message;
    }

    public void WriteLogResultNotTrader(int matchId, ResultDetail result, List<Score> scores)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var traderScore = TraderScore(scores);
        var errorMessage = $"Failed Auto Payout: Result({result.ResourceId}) " + (string.IsNullOrEmpty(traderScore) ? "no trader" : $" {traderScore} second value is invalid");

        writeLogService.WriteLog(cloneMatch, errorMessage);
    }

    public async Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails, string remark = "")
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = resultDetails };
        await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed, remark);
    }

    private static List<string> ProviderScore(IEnumerable<Score> scores)
    {
        var provider = scores.Where(s => s.SourceId > 0 && s.SourceId != SourceId.Screenshot).ToList();
        return provider.Any() ?
            provider.Select(p => ScoreMessage(p, p.SourceName)).ToList()
            : new List<string>();
    }

    private static string TraderScore(IEnumerable<Score> scores)
    {
        var trader = scores.FirstOrDefault(r => r.SourceId == SourceId.Trader);

        return trader is not null ? ScoreMessage(trader, "Trader") : string.Empty;
    }

    private static string ScoreMessage(Score score, string preFix)
    {
        var awayValue = !string.IsNullOrEmpty(score.AwayValue) ? '-' + score.AwayValue : string.Empty;
        return $"{preFix}({score.HomeValue}{awayValue})";
    }

    public void WriteLogResult(int matchId, ResultDetail result, string message)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };

        writeLogService.WriteLog(cloneMatch, message);
    }

    private async Task<IEnumerable<ResourceIndex>> GetResourceIndex(int sportType)
    {
        var criteria = new GetResourceIndexCriteria(sportType);

        var indexes = await repository.FetchAsync<ResourceIndex>(criteria);
        return indexes.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }

    public async Task<(List<ResultDetail>, List<ResourceIndex>)> GetResultsChecking(Match match, int maxResultIndex, List<ResultDetail> resultSettled)
    {
        var resultInBatch = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();

        var results = resultSettled.Concat(resultInBatch).ToList();

        var indexes = (await GetResourceIndex(match.SportType)).ToList();

        results.ForEach(s => s.IntervalNo = indexes.First(i => i.ResourceId == s.ResourceId).IntervalNo);

        var resultsChecking = results.Where(r => r.IntervalNo <= maxResultIndex).OrderBy(s => s.IntervalNo).ToList();

        return (resultsChecking, indexes);
    }

    public async Task<List<ResultDetail>> GetResultSettled(int matchId, int sportType)
    {
        var intervalDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria { MatchId = matchId, SportType = sportType })).ToList();

        return intervalDetails.Where(r => r.ResourceId != ResourceIds.FullTime).Where(r => r.ResultStatusId is ResultStatusId.ManualCompleted or ResultStatusId.AutoCompleted).ToList();
    }
}