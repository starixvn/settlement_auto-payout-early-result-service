using Fanex.Settlement.Converter.Services;

namespace AutoPayout.EarlyResult.Services;

public interface CreateQueryService
{
    SettleCriteria QuerySettle(Match match);
}

public class CreateQueryServiceImp(XmlConvertService xmlConvertService) : CreateQueryService
{
    public SettleCriteria QuerySettle(Match match)
    {
        var resourcesResult = CreateResources(match.ResultDetails);
        var criteria = new SettleCriteria
        {
            MatchId = match.MatchId,
            SelectedResources = resourcesResult,
            AutoSourceInfo = GetExternalInfo(match.ResultDetails.ToList())
        };

        return criteria;
    }

    private static string GetExternalInfo(IEnumerable<ResultDetail> resultDetails)
    {
        var groupedItems = resultDetails
            .SelectMany(r => r.ExternalSourceInfo)
            .GroupBy(item => (item.SourceId, item.SourceName)).ToList();

        var provider = GetGroupedInfo(groupedItems);

        return provider;
    }

    private static string GetGroupedInfo(IEnumerable<IGrouping<(int, string), ExternalSourceInfo>> groupedItems)
    {
        var resourceIds = string.Join(", ", groupedItems
            .Where(group => group.Key.Item1 > 0 && group.Key.Item1 != SourceId.Screenshot)
            .Select(group =>
            {
                var resourceIds = string.Join(", ", group.Select(item => item.ResourceId));
                return $"{group.Key.Item2}({resourceIds})";
            }));

        return $"Provider: {resourceIds}";
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