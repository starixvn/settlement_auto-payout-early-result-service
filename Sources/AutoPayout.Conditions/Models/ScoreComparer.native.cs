namespace AutoPayout.Conditions.Models;

public class ScoreComparer(bool compareOneValue = false) : IEqualityComparer<Score>
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

        return compareOneValue
            ? string.Compare(first.HomeValue, second.HomeValue, StringComparison.InvariantCultureIgnoreCase) == 0
            : string.Compare(first.HomeValue, second.HomeValue, StringComparison.InvariantCultureIgnoreCase) == 0
                && string.Compare(first.AwayValue, second.AwayValue, StringComparison.InvariantCultureIgnoreCase) == 0;
    }

    public int GetHashCode(Score score) => score.ResourceId.GetHashCode();
}