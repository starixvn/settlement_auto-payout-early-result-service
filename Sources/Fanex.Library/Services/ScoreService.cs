using Fanex.AutoPayoutSettlement.Core.Constants;
using Fanex.AutoPayoutSettlement.Core.Models;
using Fanex.AutoPayoutSettlement.Core.Services;
using Fanex.Data.Repository;
using Fanex.Library.Constants;
using Fanex.Library.Criteria;
using Fanex.Library.Models;
using Match = Fanex.AutoPayoutSettlement.Core.Models.Match;

namespace Fanex.Library.Services;

public interface ScoreService
{
    Task<IEnumerable<Score>> GetScore(Match match);

    void WriteLogResultInvalid(int matchId, ResultDetail result, IGrouping<string, Score> groupScore);

    Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails);
}

public class ScoreServiceImp : ScoreService
{
    private readonly IDynamicRepository repository;

    private readonly WriteLogService writeLogService;

    private readonly UpdateResultStatusService updateResultStatusService;

    public ScoreServiceImp(IDynamicRepository repository, WriteLogService writeLogService, UpdateResultStatusService updateResultStatusService)
    {
        this.repository = repository;
        this.writeLogService = writeLogService;
        this.updateResultStatusService = updateResultStatusService;
    }

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

    public void WriteLogResultInvalid(int matchId, ResultDetail result, IGrouping<string, Score> groupScore)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = new List<ResultDetail> { result } };
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, result.ResourceId);
        var traderScore = TraderScore(groupScore);
        var sourceScore = ProviderScore(groupScore);
        sourceScore.Insert(0, traderScore);

        writeLogService.WriteLog(cloneMatch, errorMessage + string.Join(", ", sourceScore));
    }

    public async Task UpdateResultStatus(int matchId, IEnumerable<ResultDetail> resultDetails)
    {
        var cloneMatch = new Match { MatchId = matchId, ResultDetails = resultDetails };
        await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed);
    }

    private static List<string> ProviderScore(IEnumerable<Score> groupScore)
    {
        var provider = groupScore.Where(s => s.SourceId > 0 && s.SourceId != SourceId.Trader).ToList();
        return provider.Any() ?
            provider.Select(p => ScoreMessage(p, p.SourceName)).ToList()
            : new List<string>();
    }

    private static string TraderScore(IEnumerable<Score> groupScore)
    {
        var trader = groupScore.FirstOrDefault(r => r.SourceId == 0);

        return trader is not null ? ScoreMessage(trader, "Trader") : string.Empty;
    }

    private static string ScoreMessage(Score score, string preFix)
    {
        return $"{preFix}({score.HomeValue}{(!string.IsNullOrEmpty(score.AwayValue) ? '-' + score.AwayValue : string.Empty)})";
    }
}