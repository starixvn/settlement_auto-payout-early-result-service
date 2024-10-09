using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Constants;
public class IgnoreResultByCondition
{
	public static readonly Dictionary<int, Func<ResultDetail, bool>> Conditions = new()
	{
		{
			SportType.BasketBall, input => input.ResourceId != ResourceIds.OverTime || input.ResourceStatus == ResourceStatus.Expired
		},
		{
			SportType.BaseBall, input => input.ResourceId != ResourceIds.ExtraInning
		}

	};
}
