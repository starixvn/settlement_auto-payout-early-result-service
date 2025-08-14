namespace AutoPayout.Conditions.Models;

public class ScoreComparerWithBetStatus : IEqualityComparer<Score>
{
    public bool Equals(Score first, Score second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        var scoresEqual = string.Compare(first.HomeValue, second.HomeValue, StringComparison.InvariantCultureIgnoreCase) == 0
                          && string.Compare(first.AwayValue, second.AwayValue, StringComparison.InvariantCultureIgnoreCase) == 0;

        if (!scoresEqual)
        {
            return false;
        }

        var firstBetStatus = string.IsNullOrEmpty(first.BetStatus) ? string.Empty : first.BetStatus;
        var secondBetStatus = string.IsNullOrEmpty(second.BetStatus) ? string.Empty : second.BetStatus;

        return string.Compare(firstBetStatus, secondBetStatus, StringComparison.InvariantCultureIgnoreCase) == 0;
    }

    public int GetHashCode(Score score)
    {
        var betStatus = string.IsNullOrEmpty(score.BetStatus) ? string.Empty : score.BetStatus;
        return HashCode.Combine(score.HomeValue, score.AwayValue, betStatus);
    }
}