namespace AutoPayout.Services;

public interface UpdateResultStatusService
{
    Task UpdateResultStatus(Match match, int status, string remark = "");
}

public class UpdateResultStatusServiceImp(IDynamicRepository repository, XmlConvertService xmlConvertService)
    : UpdateResultStatusService
{
    public async Task UpdateResultStatus(Match match, int status, string remark = "")
    {
        var resources = CreateResources(match.ResultDetails);

        var criteria = new UpdateAutoPayoutResultCriteria
        {
            MatchId = match.MatchId,
            SelectedResources = resources,
            StatusId = status,
            Remark = remark
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