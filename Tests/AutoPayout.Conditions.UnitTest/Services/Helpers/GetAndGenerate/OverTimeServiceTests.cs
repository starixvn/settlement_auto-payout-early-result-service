using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers.GetAndGenerate;
public class OverTimeServiceTests
{
    private readonly ResultDetailService resultDetailService;

    private readonly OverTimeService service;

    private readonly ResultDetail final;

    public OverTimeServiceTests()
    {
        var appSetting = new AppSettingsKey { RetryProcessScore = 300 };
        var option = Options.Create(appSetting);
        resultDetailService = Substitute.For<ResultDetailService>();
        service = new OverTimeServiceImp(resultDetailService, option);
        final = new ResultDetail()
        {
            ResourceId = ResourceIds.FullTime,
            MatchId = 1,
            SportType = 2,
        };
    }

    [Fact]
    public async Task GetOtBasketBall_QuartersNotDrawAndExistScoreOt_ReturnOtInvalid()
    {
        var score = new List<Score>()
        {
            new()
            {
                HomeValue = "1",
                AwayValue = "2",
                ResourceId = ResourceIds.OverTime
            }
        };

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(score);

        var overTime = await service.GetOtBasketBall(final, 5, 6);

        Assert.False(overTime.IsValid);
        Assert.Null(overTime.HomeValue);
        Assert.Null(overTime.AwayValue);
    }
    [Fact]
    public async Task GetOtBasketBall_QuartersNotDrawAndNotExistOT_ReturnOtValid()
    {
        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(new List<Score>());

        var overTime = await service.GetOtBasketBall(final, 5, 6);

        Assert.True(overTime.IsValid);
        Assert.Null(overTime.HomeValue);
        Assert.Null(overTime.AwayValue);
    }

    [Fact]
    public async Task GetOtBasketBall_QuartersDrawAndNotExistOT_ReturnOtInValid()
    {
        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(new List<Score>());

        var overTime = await service.GetOtBasketBall(final, 5, 5);

        Assert.False(overTime.IsValid);
        Assert.Null(overTime.HomeValue);
        Assert.Null(overTime.AwayValue);
    }

    [Fact]
    public async Task GetOtBasketBall_QuartersDrawAndExistUniqueOTScore_ReturnOtInValid()
    {

        var scores = new List<Score>()
        {
            new()
            {
                HomeValue = "1",
                AwayValue = "2",
                ResourceId = ResourceIds.OverTime,
                SourceId = 1
            },
            new()
            {
                HomeValue = "1",
                AwayValue = "2",
                ResourceId = ResourceIds.OverTime
            }
        };

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);

        var overTime = await service.GetOtBasketBall(final, 5, 5);

        Assert.True(overTime.IsValid);
        Assert.Equal("1", overTime.HomeValue);
        Assert.Equal("2", overTime.AwayValue);
    }

    [Fact]
    public async Task GetOtBasketBall_QuartersDrawAndExistNotUniqueOTScore_ReturnOtInValidAndFlagNeedRetryBaseOnCreateDate()
    {

        var scores = new List<Score>()
        {
            new()
            {
                HomeValue = "1",
                AwayValue = "2",
                ResourceId = ResourceIds.OverTime,
                SourceId = 1
            },
            new()
            {
                HomeValue = "1",
                AwayValue = "3",
                ResourceId = ResourceIds.OverTime
            }
        };

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);

        var overTime = await service.GetOtBasketBall(final, 5, 5);

        Assert.False(overTime.IsValid);
        Assert.Null(overTime.HomeValue);
        Assert.Null(overTime.AwayValue);
    }
}
