using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifySettledRelateResult : ICondition
{
    private readonly IDynamicRepository repository;

    public VerifySettledRelateResult(IDynamicRepository repository)
    {
        this.repository = repository;
    }

    public async Task HandleCondition(Match match)
    {
        await CheckRelatedResults(match);

        match.ResultDetails = match.ResultDetails.Where(x => x.IsValid).ToList();

        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private async Task CheckRelatedResults(Match match)
    {
        var relatedResults = await GetRelatedResults(match);

        foreach (var item in match.ResultDetails)
        {
            if (!HasRelatedResources(item))
            {
                continue;
            }

            var correctRelatedResults = relatedResults.Where(x
                    => item.RelatedCalResourceIds.Contains(x.ResourceId) || item.RelatedSettResourcesIds.Contains(x.ResourceId))
                .ToList();

            var isValid = ValidateResultDetail(match, item, correctRelatedResults);

            item.IsValid = isValid;

        }
    }

    private async Task<List<ResultDetail>> GetRelatedResults(Match match)
    {
        var relatedCalResourceIds = match.ResultDetails
            .SelectMany(x => x.RelatedCalResourceIds)
            .Distinct()
            .ToList();

        var relatedSettResourcesIds = match.ResultDetails
            .SelectMany(x => x.RelatedSettResourcesIds)
            .Distinct()
            .ToList();

        if (relatedCalResourceIds.Count == 0 && relatedSettResourcesIds.Count == 0)
        {
            return [];
        }

        var criteria = new CheckRelatedResultsCriteria
        {
            MatchId = match.MatchId,
            SportType = (short)match.SportType,
            RelatedCalResources = string.Join(",", relatedCalResourceIds),
            RelatedSettResources = string.Join(",", relatedSettResourcesIds)
        };

        var relatedResults = (await repository.FetchAsync<ResultDetail>(criteria)).ToList();
        return relatedResults;
    }

    private bool ValidateResultDetail(Match match, ResultDetail item, List<ResultDetail> relatedResults)
    {
        var isExistsRelatedResults = TryGetAllExistsResults(match.ResultDetails, relatedResults.ToList(), out var existingResourceIds);

        var checkingRelatedResults = relatedResults
            .Where(x => !existingResourceIds.Contains(x.ResourceId))
            .ToList();

        var isCalRelatedResults = IsCalRelatedResults(checkingRelatedResults, item.RelatedCalResourceIds);
        var isSettleRelatedResults = IsSettleRelatedResults(checkingRelatedResults, item.RelatedSettResourcesIds);

        return isExistsRelatedResults || (isCalRelatedResults && isSettleRelatedResults);
    }

    private bool HasRelatedResources(ResultDetail item)
        => item.RelatedSettResourcesIds.Count > 0 || item.RelatedCalResourceIds.Count > 0;



    private bool TryGetAllExistsResults(List<ResultDetail> settlingResults, List<ResultDetail> relatedResults, out List<string> existingResourceIds)
    {
        var settlingResultsIds = settlingResults.Select(x => x.ResourceId).ToList();

        existingResourceIds = relatedResults
            .Where(x => settlingResultsIds.Contains(x.ResourceId))
            .Select(x => x.ResourceId).ToList();

        return existingResourceIds.Count == relatedResults.Count;
    }


    private bool IsCalRelatedResults(List<ResultDetail> relatedResults, List<string> relatedCalResourceIds)
    {
        return relatedCalResourceIds.Count == 0
            || relatedResults.TrueForAll(x => relatedCalResourceIds.Contains(x.ResourceId) && x.SettlementStatusId == SettlementStatusId.Calculated);
    }


    private bool IsSettleRelatedResults(List<ResultDetail> relatedResults, List<string> relatedSettResourcesIds)
    {
        return relatedSettResourcesIds.Count == 0
            || relatedResults.TrueForAll(x => relatedSettResourcesIds.Contains(x.ResourceId) && (x.ResultStatusId is ResultStatusId.ManualCompleted or ResultStatusId.AutoCompleted));
    }
}
