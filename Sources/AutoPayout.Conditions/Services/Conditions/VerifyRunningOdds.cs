using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyRunningOdds(IDynamicRepository repository) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var criteria = CreateCriteria(match);

        await repository.ExecuteAsync(criteria);

        if (criteria.ErrCode > 0)
        {
            throw new RunningOddsException();
        }
    }

    private static VerifyOddsCriteria CreateCriteria(Match match)
    {
        var resourceIds = match.ResultDetails.Select(r => r.ResourceId).ToList();
        var checkOddsItem = new CheckOddsItem
        {
            MatchId = match.MatchId,
            ResourceIds = resourceIds
        };

        var resources = new Resources<CheckOddsItem>
        {
            ResourceItems = [checkOddsItem]
        };

        var converter = new XmlConvertServiceImp();

        var selectedResource = converter.Serialize(resources);

        return new VerifyOddsCriteria
        {
            MatchId = match.MatchId,
            SelectedResource = selectedResource,
            Action = ActionName.Calculate
        };
    }
}