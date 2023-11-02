using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.AutoPayoutSettlement.Core.Models;
using Fanex.Library.Services;

namespace Fanex.Library.Conditions;

public class VerifyConfirmScore : Condition
{
    private readonly ScoreService scoreService;

    public VerifyConfirmScore(ScoreService scoreService)
    {
        this.scoreService = scoreService;
    }

    public async Task HandleCondition(Match match)
    {
        var scores = await scoreService.GetScore(match);

        var groupedScores = scores.GroupBy(s => s.ResourceId).ToList();

        groupedScores.ForEach(group =>
        {
            var resultDetail = match.ResultDetails.First(r => r.ResourceId == group.Key);
            if (group.DistinctBy(s => new { s.HomeValue, s.AwayValue }).Count() == 1)
            {
                resultDetail.HomeValue = group.First().HomeValue;
                resultDetail.AwayValue = group.First().AwayValue;
                resultDetail.ExternalSourceInfo = group.Select(g => new ExternalSourceInfo
                { SourceName = g.SourceName, ResourceId = g.ResourceId, SourceId = g.SourceId });
            }
            else
            {
                scoreService.WriteLogResultInvalid(match.MatchId, resultDetail, group);
                resultDetail.IsValid = false;
            }
        });

        await scoreService.UpdateResultStatus(match.MatchId, match.ResultDetails.Where(r => !r.IsValid));
        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid).ToList();

        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }
    }
}