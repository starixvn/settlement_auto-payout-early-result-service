using AutoPayout.Constants;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyRelatePlayerResult(IDynamicRepository repository, IMemoryCache memoryCache) : AbstractCondition
{
    public override async Task HandleCondition(Match match)
    {
        var playerResults = GetPlayerResults(match.ResultDetails);

        if (playerResults.Count == 0)
        {
            await HandleNextCondition(match);
            return;
        }

        await ValidatePlayerResults(match, playerResults);

        match.ResultDetails = [.. match.ResultDetails.Where(x => x.IsValid)];

        await HandleNextCondition(match);
    }

    private async Task ValidatePlayerResults(Match match, List<ResultDetail> playerResults)
    {
        var oddFlexs = await GetOddFlex(match.MatchId);
        var relatedDic = playerResults.Select(x => GenerateRelatedResourceIds(x, oddFlexs)).ToDictionary();
        var relatedResults = await GetAllRelatedResults(match, relatedDic);

        foreach (var item in playerResults)
        {
            item.OddFlex = [.. oddFlexs
                .Where(o => o.ResourceId == item.ResourceId && o.BetType is not null)
                .Distinct()];

            if (!BetTypes.PlayerDependentResources.Exists(item.ResourceId.StartsWith))
            {
                continue;
            }

            var relatedCalResources = relatedDic.GetValueOrDefault(item.ResourceId) ?? [];
            var isValid = IsPlayerResultValid(relatedCalResources, relatedResults);

            item.IsValid = isValid;
            item.RelatedCalResources = isValid ? string.Join(",", relatedCalResources) : string.Empty;
            item.RelatedResults = isValid
                ? [.. relatedResults.Where(r => relatedCalResources.Contains(r.ResourceId))]
                : [];
        }
    }

    private static bool IsPlayerResultValid(List<string> itemRelatedResourceIds, List<ResultDetail> allRelatedResults)
    {
        var relatedResults = allRelatedResults
            .Where(r => itemRelatedResourceIds.Contains(r.ResourceId))
            .ToList();

        return itemRelatedResourceIds.Count == relatedResults.Count
            && relatedResults.TrueForAll(r => r.SettlementStatusId == SettlementStatusId.Calculated);
    }

    private async Task<List<ResultDetail>> GetAllRelatedResults(Match match, Dictionary<string, List<string>> relatedDictionary)
    {
        var allRelatedResourceIds = relatedDictionary.Values
            .SelectMany(x => x)
            .Distinct()
            .ToList();

        var result = await GetRelatedResults(match, string.Join(",", allRelatedResourceIds));

        return result;
    }

    private static List<ResultDetail> GetPlayerResults(List<ResultDetail> results)
        => [.. results.Where(x => BetTypes.Player.Exists(p => x.ResourceId.StartsWith(p)))];

    private async Task<List<OddFlexModel>> GetOddFlex(int matchId)
    {
        var cacheKey = $"OddFlex: {matchId}";

        var result = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            return await FetchAndParseOddFlexData(matchId);
        });

        return result;
    }

    private async Task<List<OddFlexModel>> FetchAndParseOddFlexData(int matchId)
    {
        var rawOddFlexModels = await FetchRawOddFlexData(matchId);

        foreach (var item in rawOddFlexModels)
        {
            try
            {
                item.Info = JsonConvert.DeserializeObject<OddsInfoModel>(item.OddsInfo);
            }
            catch
            {
                item.Info = null;
            }
        }

        return [.. rawOddFlexModels.Where(x => x.Info is not null)];
    }

    private async Task<List<OddFlexModel>> FetchRawOddFlexData(int matchId)
    {
        var criteria = new GetPlayerInfoCriteria
        {
            MatchId = matchId,
            SportType = SportType.BasketBall,
        };

        var oddFlexModels = await repository.FetchAsync<OddFlexModel>(criteria);
        return [.. oddFlexModels.Distinct()];
    }

    private static (string, List<string>) GenerateRelatedResourceIds(ResultDetail resultDetail, List<OddFlexModel> oddFlexs)
    {
        var foundOddFlexs = oddFlexs.Where(o => o.ResourceId == resultDetail.ResourceId && o.BetType is not null).ToList();
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

        var validRelatedResourceIds = FromStatisticToResourceId(foundOddFlexs, relatedResourceIds);

        return (resultDetail.ResourceId, validRelatedResourceIds);
    }

    private static List<string> FromStatisticToResourceId(List<OddFlexModel> oddFlexs, List<string> relatedResourceIds)
    {
        var statisticToResourceId = new Dictionary<string, string>
        {
            { "points", "653" },
            { "rebounds", "659"},
            { "assists", "656" },
            { "steals", "667" },
            { "blocks", "665" },
            { "three_points", "662" }
        };

        var statisticResourceIds = oddFlexs
            .SelectMany(o => o.Info.Statistics)
            .Where(s => statisticToResourceId.ContainsKey(s.Type))
            .Select(s => statisticToResourceId[s.Type])
            .Distinct()
            .ToList();

        return statisticResourceIds.Count == 0
            ? relatedResourceIds
            : [.. relatedResourceIds.Where(x => statisticResourceIds.Exists(z => x.StartsWith(z)) || x == ResourceIds.FullTime)];
    }

    private static List<OddFlexModel> FindMatchingOddFlex(List<OddFlexModel> oddFlex, string relatedPlayers, string betType)
    {
        const int bettypeIndex = 3;
        return [.. oddFlex
            .Where(o => relatedPlayers.Contains(o.Info.PlayerId.ToString()) && o.ResourceId[..bettypeIndex] == betType)
            .DistinctBy(r => r.ResourceId)];
    }

    private async Task<List<ResultDetail>> GetRelatedResults(Match match, string relatedCalResourceIds)
    {
        var criteria = new CheckRelatedResultsCriteria
        {
            MatchId = match.MatchId,
            SportType = (short)match.SportType,
            RelatedCalResources = relatedCalResourceIds,
            RelatedSettResources = string.Empty
        };

        var relatedResults = (await repository.FetchAsync<ResultDetail>(criteria)).ToList();
        return relatedResults;
    }
}