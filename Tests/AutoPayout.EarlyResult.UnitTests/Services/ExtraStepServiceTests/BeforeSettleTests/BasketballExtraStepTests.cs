using AutoPayout.Conditions.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;
using AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.BeforeSettleTests;
public class BasketballExtraStepTests
{
    private readonly BasketballExtraStep service = new();

    [Fact]
    public void Process_NotMainResource_StopAndReturn()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.EarlyResource
        };

        var criteria = new SettleCriteria();

        service.Process(match, criteria);

        Assert.Equal(0, criteria.DeterminedType);
        Assert.Null(criteria.BetStatus);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(17)]
    [InlineData(69)]
    public void Process_BitInstantIsOff_ReturnDeterminedTypeZero(byte AutoSetting)
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { AutoSetting = AutoSetting }
        };

        var criteria = new SettleCriteria();

        service.Process(match, criteria);

        Assert.Equal(0, criteria.DeterminedType);
        Assert.NotEmpty(criteria.BetStatus);
    }

    [Fact]
    public void Process_BitInstantIsOn_ReturnDeterminedType()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { AutoSetting = 2 },
            ResultDetails = new List<ResultDetail>() { new() { ResourceId = "Q1" } }
        };

        var criteria = new SettleCriteria();

        service.Process(match, criteria);

        Assert.NotEqual(0, criteria.DeterminedType);
        Assert.NotEmpty(criteria.BetStatus);
    }

    [Fact]
    public void Process_BoFour_ReturnBetStatus()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { AutoSetting = 2, GameSession = GameType.Bo4 },
            ResultDetails = new List<ResultDetail>() { new() { ResourceId = "Q1", HomeValue = "1", AwayValue = "2" } },
        };

        var criteria = new SettleCriteria();

        service.Process(match, criteria);

        Assert.NotEqual(0, criteria.DeterminedType);
        Assert.NotEmpty(criteria.BetStatus);
        Assert.NotEqual(BasketBallHelperService.DefaultBetStatus, criteria.BetStatus);
    }


    [Fact]
    public void Process_NotBoFour_ReturnBetStatusDefault()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { AutoSetting = 2 },
            ResultDetails = new List<ResultDetail>() { new() { ResourceId = "Q1", HomeValue = "1", AwayValue = "2" } }
        };

        var criteria = new SettleCriteria();

        service.Process(match, criteria);

        Assert.NotEqual(0, criteria.DeterminedType);
        Assert.Equal(BasketBallHelperService.DefaultBetStatus, criteria.BetStatus);
    }
}
