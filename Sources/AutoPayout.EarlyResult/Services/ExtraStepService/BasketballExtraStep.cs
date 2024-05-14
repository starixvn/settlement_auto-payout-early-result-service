using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public class BasketballExtraStep : IExtraStepService
{
    public Task Process(Match match, SettleCriteria settleCriteria)
    {
        var instantBit = 1 << 1;

        if ((match.AutoSetting & instantBit) != 0)
        {
            settleCriteria.determinedType = GenerateDeterminedType(match.ResultDetails);
        }
    }


    private byte GenerateDeterminedType(IEnumerable<ResultDetail> resultDetails)
    {
        var roundBitStartAt = 3;

        var determinedType = resultDetails.Select(resultDetail => resultDetail.ResourceId switch
            {
                ResourceIds.FullTime => 0,
                ResourceIds.Halftime => 2,
                _ => Array.FindIndex(ResourceIds.ResultBasketBall, r => r == resultDetail.ResourceId) + roundBitStartAt
            })
            .Select(bitPosition => Math.Pow(2, bitPosition))
            .Sum();

        return (byte)determinedType;
    }
}
