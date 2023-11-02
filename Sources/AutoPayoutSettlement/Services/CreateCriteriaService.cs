using Fanex.AutoPayoutSettlement.Core.Constants;
using Fanex.AutoPayoutSettlement.Core.Criteria;
using Fanex.AutoPayoutSettlement.Core.Models;
using Fanex.Settlement.Converter.Services;

namespace AutoPayoutSettlement.Services;

public interface CreateQueryService
{
    SettleCriteria QuerySettle(Match match);
}

public class CreateQueryServiceImp : CreateQueryService
{
    private readonly XmlConvertService xmlConvertService;

    public CreateQueryServiceImp(XmlConvertService xmlConvertService)
    {
        this.xmlConvertService = xmlConvertService;
    }

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

        var provider = GetGroupedInfo(groupedItems, group => group.Key.Item1 > 0 && group.Key.Item1 != SourceId.Trader, "Provider");
        var trader = GetGroupedInfo(groupedItems, group => group.Key.Item1 == 0, "Trader");
        var dash = string.IsNullOrEmpty(trader) || string.IsNullOrEmpty(provider) ? string.Empty : " - ";

        return $"{trader}{dash}{provider}";
    }

    private static string GetGroupedInfo(
            IReadOnlyCollection<IGrouping<(int, string),
            ExternalSourceInfo>> groupedItems, Func<IGrouping<(int, string), ExternalSourceInfo>,
            bool> condition, string prefix)
    {
        if (groupedItems.Any())
        {
            return $"{prefix}: " + string.Join(", ", groupedItems
                .Where(condition)
                .Select(group =>
                {
                    var resourceIds = string.Join(", ", group.Select(item => item.ResourceId));
                    return $"{group.Key.Item2}({resourceIds})";
                }));
        }

        return string.Empty;
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