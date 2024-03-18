using AutoPayout.Constants;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.Conditions.Services;

public interface ResultDetailService
{
    Task<IEnumerable<Score>> GetScore(Match match);

    void WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores);

    void WriteLogResult(int matchId, ResultDetail result, string message);

    Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails);

    Task<(List<ResultDetail>, List<ResourceIndex>)> GetResultsChecking(Match match, int maxResultIndex);
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

    public void WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, result.ResourceId);
        var traderScore = TraderScore(scores);
        var sourceScore = ProviderScore(scores);

        if (!string.IsNullOrEmpty(traderScore))
        {
            sourceScore.Insert(0, traderScore);
        }

        writeLogService.WriteLog(cloneMatch, errorMessage + string.Join(", ", sourceScore));
    }

    public async Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = resultDetails };
        await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed);
    }

    private static List<string> ProviderScore(IEnumerable<Score> scores)
    {
        var provider = scores.Where(s => s.SourceId > 0 && s.SourceId != SourceId.Trader).ToList();
        return provider.Any() ?
            provider.Select(p => ScoreMessage(p, p.SourceName)).ToList()
            : new List<string>();
    }

    private static string TraderScore(IEnumerable<Score> scores)
    {
        var trader = scores.FirstOrDefault(r => r.SourceId == 0);

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
        return indexes.Where(r => r.ResourceId != ResourceId.Final).ToList();
    }

    public async Task<(List<ResultDetail>, List<ResourceIndex>)> GetResultsChecking(Match match, int maxResultIndex)
    {
        var resultSettled = await GetResultSettled(match);

        var resultInBatch = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final).ToList();

        var results = resultSettled.Concat(resultInBatch).ToList();

        var indexes = (await GetResourceIndex(match.SportType)).ToList();

        results.ForEach(s => s.IntervalNo = indexes.First(i => i.ResourceId == s.ResourceId).IntervalNo);

        var resultOrdered = results.Where(r => r.IntervalNo <= maxResultIndex).OrderBy(s => s.IntervalNo).ToList();

        return (resultOrdered, indexes);
    }

    private async Task<List<ResultDetail>> GetResultSettled(Match match)
    {
        var intervalDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria { MatchId = match.MatchId, SportType = match.SportType })).ToList();

        return intervalDetails.Where(r => r.ResourceId != ResourceId.Final).Where(r => r.ResultStatusId is ResultStatusId.ManualCompleted or ResultStatusId.AutoCompleted).ToList();
    }
}