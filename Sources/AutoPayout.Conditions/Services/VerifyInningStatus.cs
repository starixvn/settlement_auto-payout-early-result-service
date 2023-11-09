namespace AutoPayout.Conditions.Services;

public class VerifyInningStatus : ICondition
{
    private readonly IDynamicRepository repository;

    private readonly ScoreService scoreService;

    private readonly WriteLogService writeLogService;

    private const int MatchTestSession = 1;

    private static readonly string[] InningOneIds = { "H1", "A1" };

    private static readonly string[] InningIds = { "H1", "A1", "H2", "A2" };

    public VerifyInningStatus(IDynamicRepository repository, ScoreService scoreService, WriteLogService writeLogService)
    {
        this.repository = repository;
        this.scoreService = scoreService;
        this.writeLogService = writeLogService;
    }

    public async Task HandleCondition(Match match)
    {
        if (match.ResourceType == ResourceType.MainResource && match.ResultDetails.Any(r => r.ResourceId == ResourceId.Final))
        {
            var getMatchInfoCriteria = new GetMatchDetailCriteria { MatchId = match.MatchId };
            var matchDetail = await repository.GetAsync<MatchDetail>(getMatchInfoCriteria);
            await ValidateInning(match, matchDetail.GameSession);
        }
    }

    private async Task ValidateInning(Match match, int gameSession)
    {
        var resourceIds = match.ResultDetails.Select(r => r.ResourceId);
        var inningsToValidate = gameSession == MatchTestSession ? InningIds : InningOneIds;
        inningsToValidate = inningsToValidate.Where(i => !resourceIds.Contains(i)).ToArray();
        if (!inningsToValidate.Any())
        {
            return;
        }

        if (await ValidateStatus(inningsToValidate, match.MatchId, match.SportType))
        {
            await UpdateFinalResourceStatus(match);
        }
    }

    private async Task<bool> ValidateStatus(string[] inningsToValidate, int matchId, int sportType)
    {
        var resultDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria { MatchId = matchId, SportType = sportType })).ToList();
        var checkInningNotExisted = inningsToValidate.Any(i => resultDetails.All(r => r.ResourceId != i));
        var checkInningRunning = inningsToValidate.Any(inning =>
            resultDetails.Any(r => inning == r.ResourceId &&
                                   (r.ResultStatusId != ResultStatusId.ManualCompleted &&
                                    r.ResultStatusId != ResultStatusId.AutoCompleted)));
        return checkInningNotExisted || checkInningRunning;
    }

    private async Task UpdateFinalResourceStatus(Match match)
    {
        var finalResource = match.ResultDetails.Where(r => r.ResourceId == ResourceId.Final).ToList();
        match.ResultDetails = match.ResultDetails.Except(finalResource);
        await scoreService.UpdateResultStatus(match.MatchId, finalResource);
        await writeLogService.WriteLog(new Match() { MatchId = match.MatchId, ResultDetails = finalResource },
            new InningNotSettledException());
    }
}