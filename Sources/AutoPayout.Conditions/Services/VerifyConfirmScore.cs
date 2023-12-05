using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services;

public class VerifyConfirmScore : ICondition
{
    private readonly ScoreService scoreService;

    public VerifyConfirmScore(ScoreService scoreService)
    {
        this.scoreService = scoreService;
    }

    public async Task HandleCondition(Match match)
    {
        var scores = await scoreService.GetScore(match);
        CompareScore(match, scores);

        if (match.ResultDetails.Any(r => !r.IsValid))
        {
            await scoreService.UpdateResultStatus(match.MatchId, match.ResultDetails.Where(r => !r.IsValid));
            match.ResultDetails = match.ResultDetails.Where(r => r.IsValid);
        }

        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private void CompareScore(Match match, IEnumerable<Score> scores)
    {
        var groupedScores = scores.GroupBy(s => s.ResourceId).ToList();

        groupedScores.ForEach(group =>
        {
            var resultDetail = match.ResultDetails.First(r => r.ResourceId == group.Key);
            if (group.Distinct(new ScoreComparer()).Count() == 1)
            {
                resultDetail.HomeValue = group.First().HomeValue;
                resultDetail.AwayValue = group.First().AwayValue;
                resultDetail.ExternalSourceInfo = group.Select(g => new ExternalSourceInfo
                { SourceName = g.SourceName, ResourceId = g.ResourceId, SourceId = g.SourceId });
            }
            else
            {
                scoreService.WriteLogResultInvalid(match.MatchId, resultDetail, group.ToList());
                resultDetail.IsValid = false;
            }
        });
    }
}