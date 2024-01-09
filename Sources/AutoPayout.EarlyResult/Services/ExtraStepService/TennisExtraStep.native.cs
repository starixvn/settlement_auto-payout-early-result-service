
using AutoPayout.EarlyResult.Criteria;
using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public class TennisExtraStep(IDynamicRepository repository) : IExtraStepService
{
    public async Task Process(SettleCriteria settleCriteria)
    {
        var criteria = new InsRefundResultOSCriteria
        {
            MatchId = settleCriteria.MatchId
        };

        await repository.ExecuteAsync(criteria);
    }
}
