using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Constants;
public class IgnoreResultByCondition
{
    public static readonly Dictionary<int, Func<string, bool>> Conditions = new()
    {
        { SportType.BasketBall, input => input != ResourceIds.OverTime }
    };
}
