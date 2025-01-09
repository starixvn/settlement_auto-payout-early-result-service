using AutoPayout.EarlyResult.Criteria;
using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
public class AddResourceRefund(IDynamicRepository repository) : IExtraStepService
{
    public async Task Process(Match match, SettleCriteria settleCriteria)
    {
        if (!string.IsNullOrEmpty(match.ResourceRefund))
        {
            var criteria = new AddResourceRefundCriteria
            {
                MatchId = match.MatchId,
                ResourceIds = match.ResourceRefund
            };
            await repository.ExecuteAsync(criteria);
        }
    }
}
