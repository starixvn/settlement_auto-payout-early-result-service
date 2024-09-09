using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyInningStatus(IDynamicRepository repository, ResultDetailService resultDetailService,
        WriteLogService writeLogService)
    : ICondition
{
    private const int MatchTestSession = 1;

    private static readonly string[] InningOneIds = ["H1", "A1"];

    private static readonly string[] InningIds = ["H1", "A1", "H2", "A2"];

    public async Task HandleCondition(Match match)
    {
        if (match.ResourceType == ResourceType.MainResource && match.ResultDetails.Exists(r => r.ResourceId == ResourceIds.FullTime))
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
        var inningNotExisted = Array.Exists(inningsToValidate, i => resultDetails.TrueForAll(r => r.ResourceId != i));
        var inningRunning = Array.Exists(inningsToValidate, inning =>
            resultDetails.Exists(r => inning == r.ResourceId &&
                                   (r.ResultStatusId != ResultStatusId.ManualCompleted &&
                                    r.ResultStatusId != ResultStatusId.AutoCompleted)));
        return inningNotExisted || inningRunning;
    }

    private async Task UpdateFinalResourceStatus(Match match)
    {
        var finalResource = match.ResultDetails.Where(r => r.ResourceId == ResourceIds.FullTime).ToList();
        match.ResultDetails = match.ResultDetails.Except(finalResource).ToList();
        await resultDetailService.UpdateResultStatus(match.MatchId, finalResource, ErrorMessage.IntervalNotSettled);
        await writeLogService.WriteLog(new Match { MatchId = match.MatchId, ResultDetails = finalResource },
            new IntervalNotSettledException());
    }
}