using AutoPayout.Constants;
using AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.HelperServices;
public class BasketBallHelperServiceTests
{
    [Fact]
    public void GenerateDeterminedType_Always_ReturnCorrectValue()
    {
        var results = ResourceIds.ResultFtBasketBall.Select(t => new ResultDetail() { ResourceId = t }).ToList();
        results.Add(new ResultDetail() { ResourceId = ResourceIds.FullTime });

        var actual = BasketBallHelperService.GenerateDeterminedType(new Match() { ResultDetails = results });

        Assert.Equal(253, actual);
    }

    [Theory]
    [InlineData("Q1", "1-2,0-0,0-0,0-0,0-0")]
    [InlineData("Q2", "1-1,1-2,0-0,0-0,0-0")]
    [InlineData("Q3", "1-1,0-0,1-2,0-0,0-0")]
    [InlineData("Q4", "1-1,0-0,0-0,1-2,0-0")]
    [InlineData("OT", "1-1,0-0,0-0,0-0,1-2")]
    public void GetBetStatusBoFour_Always_ReturnEnoughAndCorrectBetStatus(string quarter, string betStatus)
    {
        var match = new Match()
        {
            MatchDetail = new MatchDetail()
            {
                BetStatus = "1-1"
            },
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = quarter,
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        var actual = BasketBallHelperService.GetBetStatusBoFour(match);

        Assert.Equal(5, actual.Split(",").Length);
        Assert.Equal(betStatus, actual);
    }

    [Fact]
    public void GetBetStatusBoTwo_FullTime_ReturnEnoughAndCorrectBetStatus()
    {
        var match = new Match()
        {
            MatchDetail = new MatchDetail()
            {
                BetStatus = "1-1"
            },
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "1",
                    AwayValue = "2",
                    RelatedInfo = "0-1"
                }
            }
        };

        var actual = BasketBallHelperService.GetBetStatusBoTwo(match);
        Assert.Equal("0-1,0-0,1-2,0-0,0-0", actual);
    }

    [Fact]
    public void GetBetStatusBoTwo_HalfTime_ReturnEnoughAndCorrectBetStatus()
    {
        var match = new Match
        {
            MatchDetail = new MatchDetail
            {
                BetStatus = "1-1"
            },
            ResultDetails = new List<ResultDetail>
            {
                new()
                {
                    ResourceId = ResourceIds.Halftime,
                    HomeValue = "1",
                    AwayValue = "2",
                }
            }
        };

        var actual = BasketBallHelperService.GetBetStatusBoTwo(match);
        Assert.Equal("1-2,0-0,0-0,0-0,0-0", actual);
    }

    [Fact]
    public void GetBetStatusBoOne_Always_ReturnEnoughAndCorrectBetStatus()
    {
        var match = new Match
        {
            MatchDetail = new MatchDetail
            {
                BetStatus = "1-1"
            },
            ResultDetails = new List<ResultDetail>
            {
                new()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "1",
                    AwayValue = "2",
                }
            }
        };

        var actual = BasketBallHelperService.GetBetStatusBoOne(match);
        Assert.Equal("0-0,0-0,1-2,0-0,0-0", actual);
    }
}
