using AutoPayout.Constants;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.Conditions.Services;

public interface ResultDetailService
{
    Task<IEnumerable<Score>> GetScore(Match match);

    void WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores);

    void WriteLogResult(int matchId, ResultDetail result, string message);

    Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails);
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

        return await repository.FetchAsync<Score>(criteria);
    }

    public void WriteLogResultInvalid(int matchId, ResultDetail result, List<Score> scores)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, result.ResourceId);
        var traderScore = TraderScore(scores);
        var sourceScore = ProviderScore(scores);
        sourceScore.Insert(0, traderScore);

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
}