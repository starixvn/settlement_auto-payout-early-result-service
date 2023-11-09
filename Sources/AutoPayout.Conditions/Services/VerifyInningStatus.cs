namespace AutoPayout.Conditions.Services;

public class VerifyInningStatus : ICondition
{
    private readonly IDynamicRepository repository;

    private const int MatchTestSession = 1;

    private const string InningOneId = "H1,A1";

    private const string InningTwoId = "H2,A2";

    public VerifyInningStatus(IDynamicRepository repository)
    {
        this.repository = repository;
    }

    public async Task HandleCondition(Match match)
    {
        if (match.ResultDetails.Any(r => r.ResourceId == "00"))
        {
            var getMatchInfoCriteria = new GetMatchDetailCriteria { MatchId = match.MatchId };
            var matchDetail = await repository.GetAsync<MatchDetail>(getMatchInfoCriteria);
            var resultDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria
            { MatchId = match.MatchId, SportType = match.SportType })).ToList();

            ValidateInning(resultDetails, matchDetail.GameSession);
        }
    }

    private static void ValidateInning(IReadOnlyCollection<ResultDetail> resultDetails, int gameSession)
    {
        var inningsToValidate = gameSession == MatchTestSession ? new List<string> { InningOneId, InningTwoId } : new List<string> { InningOneId };

        if (inningsToValidate.Any(inning =>
                resultDetails.Any(r => inning.Contains(r.ResourceId) &&
                                       (r.ResultStatusId != ResultStatusId.ManualCompleted && r.ResultStatusId != ResultStatusId.AutoCompleted))))
        {
            throw new InningNotSettledException();
        }
    }
}