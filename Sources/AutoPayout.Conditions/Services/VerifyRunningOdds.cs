namespace AutoPayout.Conditions.Services;

public class VerifyRunningOdds : ICondition
{
    private readonly IDynamicRepository repository;

    public VerifyRunningOdds(IDynamicRepository repository)
    {
        this.repository = repository;
    }

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
            ResourceItems = new List<CheckOddsItem> { checkOddsItem }
        };

        var converter = new XmlConvertServiceImp();

        var selectedResource = converter.Serialize(resources);

        return new VerifyOddsCriteria
        {
            MatchId = match.MatchId,
            SelectedResource = selectedResource,
            Action = "CAL"
        };
    }
}