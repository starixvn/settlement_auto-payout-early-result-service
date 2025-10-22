using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyRelatePlayerResult(IDynamicRepository repository) : AbstractCondition
{
    public static List<string> DependentResources => ["669", "671", "673", "674", "675", "676", "677", "678", "679", "680", "681", "682", "683", "684"];

    public override async Task HandleCondition(Match match)
    {
        var playerResults = match.ResultDetails
            .Where(x => DependentResources.Exists(p => x.ResourceId.StartsWith(p)))
            .ToList();

        if (playerResults.Count == 0)
        {
            await HandleNextCondition(match);
            return;
        }

        var oddFlexs = await GetOddFlex(match.MatchId);
        var relatedDic = playerResults.Select(x => GenerateRelatedResourceIds(x, oddFlexs)).ToDictionary();
        var relatedResourceIds = relatedDic.Values.SelectMany(x => x).Distinct().ToList();
        var relatedResults = await GetRelatedResults(match, string.Join(",", relatedResourceIds));

        foreach (var item in playerResults)
        {
            var itemRelatedResourceIds = relatedDic.GetValueOrDefault(item.ResourceId);
            var x = RelatedResultsHelper.TryGetAllExistsResults(
                [.. match.ResultDetails.Select(x => x.ResourceId)],
                itemRelatedResourceIds,
                out var existingResourceIds);

            var itemRelatedResults = relatedResults
                .Where(x => itemRelatedResourceIds.Contains(x.ResourceId) && !existingResourceIds.Contains(x.ResourceId))
                .ToList();

            var y = itemRelatedResults.TrueForAll(x => x.SettlementStatusId == SettlementStatusId.Calculated);
        }


    }


    private async Task<IEnumerable<OddFlexModel>> GetOddFlex(int matchId)
    {
        var criteria = new GetPlayerInfoCriteria
        {
            MatchId = matchId,
            SportType = SportType.BasketBall,
        };

        var oddFlex = (await repository.FetchAsync<OddFlexModel>(criteria))
            .Distinct()
            .ToList();

        return oddFlex;
    }

    private static (string, List<string>) GenerateRelatedResourceIds(ResultDetail dependentResource, IEnumerable<OddFlexModel> oddFlexs)
    {
        var foundOddFlexs = oddFlexs.Where(o => o.ResourceId == dependentResource.ResourceId && o.BetType is not null).ToList();
        var relatedBetTypes = foundOddFlexs.SelectMany(r => r.RelatedBettype.Split(',')).Distinct();
        var relatedPlayers = string.Join(',', foundOddFlexs.Select(r => r.Info.PlayerId));

        var relatedResourceIds = new List<string>();

        foreach (var betType in relatedBetTypes)
        {
            if (betType == ResourceType.Final)
            {
                relatedResourceIds.Add(ResourceType.Final);
                continue;
            }
            var matchingOddFlex = FindMatchingOddFlex([.. oddFlexs], relatedPlayers, betType);
            relatedResourceIds.AddRange(matchingOddFlex.Select(r => r.ResourceId));
        }

        return (dependentResource.ResourceId, relatedResourceIds);
    }

    private static List<OddFlexModel> FindMatchingOddFlex(List<OddFlexModel> oddFlex, string relatedPlayers, string betType)
    {
        const int bettypeIndex = 3;
        return oddFlex
            .Where(o => relatedPlayers.Contains(o.Info.PlayerId.ToString()) && o.ResourceId[..bettypeIndex] == betType)
            .DistinctBy(r => r.ResourceId)
            .ToList();
    }

    private async Task<List<ResultDetail>> GetRelatedResults(Match match, string relatedCalResourceIds)
    {
        var criteria = new CheckRelatedResultsCriteria
        {
            MatchId = match.MatchId,
            SportType = (short)match.SportType,
            RelatedCalResources = relatedCalResourceIds
        };

        var relatedResults = (await repository.FetchAsync<ResultDetail>(criteria)).ToList();
        return relatedResults;
    }
}