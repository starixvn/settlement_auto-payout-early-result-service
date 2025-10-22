namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;

public static class RelatedResultsHelper
{
    public static bool TryGetAllExistsResults(List<ResultDetail> settlingResults, List<ResultDetail> relatedResults, out List<string> existingResourceIds)
    {
        List<string> settlingResultsIds = [.. settlingResults.Select(x => x.ResourceId)];
        List<string> relatedResultIds = [.. relatedResults.Select(x => x.ResourceId)];

        return TryGetAllExistsResults(settlingResultsIds, relatedResultIds, out existingResourceIds);
    }

    public static bool TryGetAllExistsResults(List<string> settlingResultIds, List<string> relatedResultIds, out List<string> existingResourceIds)
    {
        existingResourceIds = [.. relatedResultIds
            .Where(settlingResultIds.Contains)
            .Select(x => x)];

        return existingResourceIds.Count == relatedResultIds.Count;
    }
}
