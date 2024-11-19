using System.Text;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifySettledRelateResult : ICondition
{
    private readonly IDynamicRepository repository;
    private readonly ResultDetailService resultDetailService;
    private readonly AppSettingsKey appSettings;

    public VerifySettledRelateResult(IDynamicRepository repository, ResultDetailService resultDetailService, IOptions<AppSettingsKey> options)
    {
        this.repository = repository;
        this.resultDetailService = resultDetailService;
        this.appSettings = options.Value;
    }

    public async Task HandleCondition(Match match)
    {
        await CheckRelatedResults(match);

        await HandleExpiredResults(match);

        match.ResultDetails = match.ResultDetails.Where(x => x.IsValid).ToList();

    }

    private async Task CheckRelatedResults(Match match)
    {
        foreach (var item in match.ResultDetails)
        {
            if (!HasRelatedResources(item)) { continue; }

            var criteria = CreateCriteria(match, item.Scores[0]);
            var relatedResults = (await repository.FetchAsync<ResultDetail>(criteria)).ToList();

            var isValid = ValidateResultDetail(match, item, relatedResults);
            item.IsValid = isValid;

            SetExpiredResult(item);
        }
    }

    private bool ValidateResultDetail(Match match, ResultDetail item, List<ResultDetail> relatedResults)
    {
        var score = item.Scores[0];

        var isExistsRelatedResults = TryGetExistsResults(match.ResultDetails, relatedResults.ToList(), out var existingResourceIds);

        var checkingRelatedResults = relatedResults
            .Where(x => !existingResourceIds.Contains(x.ResourceId))
            .ToList();

        var isCalRelatedResults = IsCalRelatedResults(checkingRelatedResults, score.RelatedCalResourceIds);
        var isSettleRelatedResults = IsSettleRelatedResults(checkingRelatedResults, score.RelatedSettResourcesIds);

        return isExistsRelatedResults || (isCalRelatedResults && isSettleRelatedResults);
    }

    private bool HasRelatedResources(ResultDetail item)
        => item.Scores.Count != 0
           && (!string.IsNullOrEmpty(item.Scores[0].RelatedSettResources) || !string.IsNullOrEmpty(item.Scores[0].RelatedCalResources));

    private CheckRelatedResultsCriteria CreateCriteria(Match match, Score score) =>
        new()
        {
            MatchId = match.MatchId,
            SportType = (short)match.SportType,
            RelatedCalResources = score.RelatedCalResources ?? string.Empty,
            RelatedSettResources = score.RelatedSettResources ?? string.Empty
        };


    private bool TryGetExistsResults(List<ResultDetail> settlingResults, List<ResultDetail> relatedResults, out List<string> existingResourceIds)
    {
        var settlingResultsIds = settlingResults.Select(x => x.ResourceId).ToList();

        existingResourceIds = relatedResults
            .Where(x => settlingResultsIds.Contains(x.ResourceId))
            .Select(x => x.ResourceId).ToList();

        return existingResourceIds.Count > 0;
    }

    private void SetExpiredResult(ResultDetail resultDetail)
    {
        var maxDate = resultDetail.Scores.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;

        resultDetail.Expired = processTime.TotalMilliseconds > appSettings.RetryProcessScore;
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


    private async Task HandleExpiredResults(Match match)
    {
        var expiredResults = match.ResultDetails
            .Where(x => !x.IsValid && x.Expired)
            .ToList();

        if (!expiredResults.Any()) { return; }

        var expiredResultsInfo = new StringBuilder();
        foreach (var result in expiredResults)
        {
            expiredResultsInfo.AppendLine(FormatExpiredResultInfo(result));
        }

        var errorMessage = string.Format(ErrorMessage.ExpiredResults, expiredResultsInfo.ToString().TrimEnd());
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, expiredResults, errorMessage);
        await repository.ExecuteAsync(new WriteLogCriteria(match.MatchId, errorMessage));
    }

    private string FormatExpiredResultInfo(ResultDetail result)
    {
        var relatedCalResources = result.Scores.FirstOrDefault()?.RelatedCalResources ?? string.Empty;
        var relatedSettleResources = result.Scores.FirstOrDefault()?.RelatedSettResources ?? string.Empty;

        var sb = new StringBuilder();
        sb.Append($"{result.ResourceId}({result.HomeValue}-{result.AwayValue}");
        if (!string.IsNullOrEmpty(relatedCalResources))
        {
            sb.Append($", RelatedCalResources: {relatedCalResources}");
        }

        if (!string.IsNullOrEmpty(relatedSettleResources))
        {
            sb.Append($", RelatedSettleResources: {relatedSettleResources}");
        }

        sb.Append(")");

        return sb.ToString();
    }
}
