using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Criteria;
using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;

public class TennisExtraStep(IDynamicRepository repository) : IExtraStepService
{
    public async Task Process(Match match, SettleCriteria criteria)
    {
        if (match.ResourceType != ResourceType.MainResource)
        {
            return;
        }


        var criteria = new InsRefundResultCriteria
        {
            MatchId = match.MatchId,
            ResourceIds = string.Join(',', match.ResultDetails.Select(r => r.ResourceId))

        };

        await repository.ExecuteAsync(criteria);
    }
}