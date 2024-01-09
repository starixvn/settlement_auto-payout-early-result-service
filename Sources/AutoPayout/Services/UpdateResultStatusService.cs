namespace AutoPayout.Services;

public interface UpdateResultStatusService
{
    Task UpdateResultStatus(Match match, int status);
}

public class UpdateResultStatusServiceImp(IDynamicRepository repository, XmlConvertService xmlConvertService)
    : UpdateResultStatusService
{
    public async Task UpdateResultStatus(Match match, int status)
    {
        var resources = CreateResources(match.ResultDetails);

        var criteria = new UpdateAutoPayoutResultCriteria
        {
            MatchId = match.MatchId,
            SelectedResources = resources,
            StatusId = status
        };

        await repository.ExecuteAsync(criteria);
    }

    private string CreateResources(IEnumerable<ResultDetail> selectedResources)
    {
        var resources = new Resources<ResultDetail>
        {
            ResourceItems = selectedResources.ToList()
        };

        return xmlConvertService.Serialize(resources);
    }
}