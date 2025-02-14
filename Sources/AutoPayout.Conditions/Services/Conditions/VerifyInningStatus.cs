using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyInningStatus(IDynamicRepository repository, ResultDetailService resultDetailService,
        WriteLogService writeLogService, IOptions<AppSettingsKey> appSettingOptions)
    : AbstractCondition
{
    private const int MatchTestSession = 1;

    private static readonly HashSet<string> InningOneIds = new(["H1", "A1"]);
    private static readonly HashSet<string> InningIds = new(["H1", "A1", "H2", "A2"]);


    public override async Task HandleCondition(Match match)
    {
        if (ShouldValidateInning(match))
        {
            await ValidateInning(match);
        }

        await HandleNextCondition(match);
    }

    private bool ShouldValidateInning(Match match)
    {
        return match.ResourceType == ResourceType.MainResource &&
               match.ResultDetails.Exists(r => r.ResourceId == ResourceIds.FullTime && match.MatchDetail.ParentID == 0);
    }


    private async Task ValidateInning(Match match)
    {
        var existingResourceIds = match.ResultDetails.Select(r => r.ResourceId).ToHashSet();
        var inningsToValidate = (match.MatchDetail.GameSession == MatchTestSession ? InningIds : InningOneIds)
                               .Except(existingResourceIds)
                               .ToArray();

        if (!inningsToValidate.Any())
        {
            return;
        }

        await ValidateStatus(match, inningsToValidate.ToList());
    }

    private async Task ValidateStatus(Match match, List<string> inningsToValidate)
    {
        var criteria = new GetResultDetailCriteria { MatchId = match.MatchId, SportType = match.SportType };

        var settledResults = (await repository.FetchAsync<ResultDetail>(criteria))
            .Where(x => x.ResultStatusId == ResultStatusId.ManualCompleted || x.ResultStatusId == ResultStatusId.AutoCompleted)
            .Select(x => x.ResourceId)
            .ToHashSet();


        bool valid = inningsToValidate.TrueForAll(settledResults.Contains);

        if (valid)
        {
            return;
        }

        var finalResult = match.ResultDetails.Find(r => r.ResourceId == ResourceIds.FullTime);
        var maxTimeScore = await resultDetailService.MaxTimeScore(finalResult);

        if (maxTimeScore > appSettingOptions.Value.RetryProcessScore) // false greater 5 min
        {
            await UpdateInvalidFinalResourceStatus(match);
            return;
        }

        match.ResultDetails.Remove(finalResult);
    }

    private async Task UpdateInvalidFinalResourceStatus(Match match)
    {
        var finalResource = match.ResultDetails.Where(r => r.ResourceId == ResourceIds.FullTime).ToList();
        match.ResultDetails = match.ResultDetails.Except(finalResource).ToList();

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, finalResource, ErrorMessage.IntervalNotSettled);
        await writeLogService.WriteLog(new Match { MatchId = match.MatchId, ResultDetails = finalResource },
            new IntervalNotSettledException());
    }

}