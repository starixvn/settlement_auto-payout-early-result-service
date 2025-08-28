using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using AutoPayout.Extensions;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalMuayThai(
    ResultDetailService resultDetailService,
    MuayThaiHelperService muayThaiHelperService,
    UpdateResultStatusService updateResultStatusService,
    ScoreMessageService scoreMessageService,
    IOptions<AppSettingsKey> options) : AbstractCondition
{
    public override async Task HandleCondition(Match match)
    {
        var scores = await resultDetailService.GetScores(match);

        if (scores.Any())
        {
            SetBetStatusForTrade(scores, match.MatchDetail.BetStatus);
            var cloneScores = scores.DeepClone();
            CompareScore(match, scores, cloneScores);
            await ProcessFailureResults(match, cloneScores);
            match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any()).ToList();
        }

        await HandleNextCondition(match);
    }

    private static void SetBetStatusForTrade(IEnumerable<Score> scores, string betStatus)
    {
        foreach (var score in scores)
        {
            if (score.SourceId == SourceId.Trader)
            {
                score.BetStatus = betStatus;
            }
        }
    }

    private async Task ProcessFailureResults(Match match, IEnumerable<Score> scores)
    {
        var failureResults = match.ResultDetails.Where(r => !r.IsValid && r.Expired).ToList();
        var failureResourceIds = failureResults.Select(x => x.ResourceId).ToList();

        if (failureResults.Count > 0)
        {
            var remark = scoreMessageService.GetScoreMessage(scores, failureResourceIds[0]);
            var cloneMatch = new Match { MatchId = match.MatchId, ResultDetails = failureResults.ToList() };
            await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed, remark);
        }
    }

    private (string settleHome, string settleAway, string settleRound, bool correctScore) MapSingleScoreWithBoxing(Score originalScore)
    {
        if (!int.TryParse(originalScore.HomeValue, out var homeValue) ||
            !int.TryParse(originalScore.AwayValue, out var awayValue))
        {
            return (originalScore.HomeValue, originalScore.AwayValue, string.Empty, false);
        }

        return muayThaiHelperService.MapSboToSettle(homeValue, awayValue);
    }

    private void CompareScore(Match match, IEnumerable<Score> scores, List<Score> cloneScores)
    {
        var scoreList = scores.ToList();
        var resultDetail = match.ResultDetails[0];

        if (scoreList.Count == 1)
        {
            ProcessSingleScore(match, resultDetail, scoreList[0]);
        }
        else
        {
            ProcessMultipleScores(match, resultDetail, scoreList, cloneScores);
        }
    }

    private void ProcessSingleScore(Match match, ResultDetail resultDetail, Score singleScore)
    {
        var (settleHome, settleAway, settleRound, correctScore) = GetSettledValues(singleScore);

        if (correctScore)
        {
            SetMatchResult(match, resultDetail, settleHome, settleAway, settleRound, [singleScore]);
        }
        else
        {
            MarkFail(resultDetail, [singleScore], match.MatchId);
        }
    }

    private void ProcessMultipleScores(Match match, ResultDetail resultDetail, List<Score> scoreList, List<Score> cloneScores)
    {
        var mappedScores = MapScoresWithBoxingService(scoreList);
        var areConsistent = AreAllScoresConsistent(mappedScores);

        if (areConsistent)
        {
            var specialScore = mappedScores[0];
            SetMatchResult(match, resultDetail, specialScore.HomeValue, specialScore.AwayValue, specialScore.BetStatus, scoreList);
        }
        else
        {
            MarkFail(resultDetail, cloneScores, match.MatchId);
        }
    }

    private (string settleHome, string settleAway, string settleRound, bool correctScore) GetSettledValues(Score singleScore)
    {
        var settleHome = singleScore.HomeValue;
        var settleAway = singleScore.AwayValue;
        var settleRound = singleScore.BetStatus;
        var correctScore = true;

        if (singleScore.SourceId != 0)
        {
            (settleHome, settleAway, settleRound, correctScore) = MapSingleScoreWithBoxing(singleScore);
        }

        return (settleHome, settleAway, settleRound, correctScore);
    }

    private static void SetMatchResult(Match match, ResultDetail resultDetail, string homeValue, string awayValue, string betStatus, IEnumerable<Score> scores)
    {
        match.MatchDetail.BetStatus = betStatus;
        resultDetail.HomeValue = homeValue;
        resultDetail.AwayValue = awayValue;
        resultDetail.ExternalSourceInfo = scores.Select(score => new ExternalSourceInfo
        {
            SourceName = score.SourceName,
            ResourceId = score.ResourceId,
            SourceId = score.SourceId
        });
    }

    private static bool AreAllScoresConsistent(IEnumerable<Score> mappedScores)
    {
        if (!mappedScores.Any())
        {
            return false;
        }

        var distinctScores = mappedScores.Distinct(new ScoreComparerWithBetStatus()).ToList();
        return distinctScores.Count == 1;
    }

    private List<Score> MapScoresWithBoxingService(IEnumerable<Score> scores)
    {
        var mappedScores = new List<Score>();

        foreach (var score in scores)
        {
            var mappedScore = MapSingleScoreWithBoxing(score);

            if (!mappedScore.correctScore)
            {
                continue;
            }

            if (score.SourceId != 0)
            {
                score.HomeValue = mappedScore.settleHome;
                score.AwayValue = mappedScore.settleAway;
                score.BetStatus = mappedScore.settleRound;
            }

            mappedScores.Add(score);
        }

        return mappedScores;
    }

    private void MarkFail(ResultDetail resultDetail, IEnumerable<Score> groupScore, int matchId)
    {
        var maxDate = groupScore.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;
        if (processTime.TotalMilliseconds > options.Value.RetryProcessScore)
        {
            resultDetailService.WriteLogResultInvalid(matchId, resultDetail, groupScore.ToList());
            resultDetail.Expired = true;
        }

        resultDetail.IsValid = false;
    }
}