using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;

public interface ScoreMessageService
{
    string GetScoreMessage(IEnumerable<Score> scores, string resourceId);
}

public class ScoreMessageServiceImp : ScoreMessageService
{
    public string GetScoreMessage(IEnumerable<Score> scores, string resourceId)
    {
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, resourceId);
        var sourceScore = ProviderScore(scores);
        var traderScore = TraderScore(scores);

        if (!string.IsNullOrEmpty(traderScore))
        {
            sourceScore.Insert(0, traderScore);
        }

        return $"{errorMessage}{string.Join(", ", sourceScore)}";
    }

    private static List<string> ProviderScore(IEnumerable<Score> scores)
    {
        var provider = scores.Where(s => s.SourceId > 0 && s.SourceId != SourceId.Screenshot).ToList();
        return provider.Count > 0
            ? provider.Select(p => ScoreMessage(p, p.SourceName)).ToList()
            : [];
    }

    private static string TraderScore(IEnumerable<Score> scores)
    {
        var trader = scores.ToList().Find(r => r.SourceId == SourceId.Trader);

        return trader is not null ? ScoreMessage(trader, "Trader") : string.Empty;
    }

    private static string ScoreMessage(Score score, string preFix)
    {
        var betStatus = !string.IsNullOrEmpty(score.BetStatus) ? $";{score.BetStatus}" : string.Empty;

        return $"{preFix}({score.HomeValue}-{score.AwayValue}{betStatus})";
    }
}