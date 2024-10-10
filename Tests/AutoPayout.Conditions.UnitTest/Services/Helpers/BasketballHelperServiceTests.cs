using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers;

public class BasketballHelperServiceTests
{
    private readonly ResultDetail halfTime = new() { ResourceId = ResourceIds.Halftime, HomeValue = "1", AwayValue = "2" };

    private readonly ResultDetail fullTime = new() { ResourceId = ResourceIds.FullTime };

    private readonly BasketballHelperServiceImp service;

    private readonly ExtraResultService overTimeService;

    private readonly ResultDetailService resultDetailService;

    public BasketballHelperServiceTests()
    {
        overTimeService = Substitute.For<ExtraResultService>();
        resultDetailService = Substitute.For<ResultDetailService>();
        var appSetting = new AppSettingsKey { RetryProcessScore = 300000 };
        var option = Options.Create(appSetting);

        var scores = new List<Score>()
        {
            new()
            {
                CreateDate = DateTime.Now.AddMinutes(-6)
            }
        };

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);

        service = new BasketballHelperServiceImp(resultDetailService, overTimeService, option);
    }

    private static List<ResultDetail> GetQuarter((int, int) quantity)
    {
        var list = new List<ResultDetail>();
        for (var i = quantity.Item1; i < quantity.Item2; i++)
        {
            list.Add(new ResultDetail() { ResourceId = ResourceIds.ResultFtBasketBall[i] });
        }

        return list;
    }

    [Fact]
    public async Task VerifyHalfTimeInValid_MatchBO1_ReturnTrueWithCorrectMessage()
    {
        var message = await service.VerifyHalfTimeInValid(new Match(), halfTime, [], GameType.Bo1);

        Assert.Equal("Failed Auto Payout: HT(1-2) due to invalid match type: GameSession(1)", message);
    }

    [Fact]
    public async Task VerifyHalfTimeInValid_MatchBO4AndRelatedResultNotValidAndMaxTimeScoreGreaterThanRetryProcessScore_ReturnsErrorMessage()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "Q1"
                },

                new ResultDetail
                {
                    ResourceId = "01"
                }
            ]
        };

        var results = GetQuarter((0, 1));
        var message = await service.VerifyHalfTimeInValid(match, halfTime, results, GameType.Bo4);
        const string expected = "Failed Auto Payout: HT(1-2) due to related result(s) not calculated: Q2";

        Assert.Equal(2, match.ResultDetails.Count);
        Assert.Equal(expected, message);
    }

    [Fact]
    public async Task VerifyHalfTimeInValid_MatchBO4AndRelatedResultNotValidAndMaxTimeScoreLessThanRetryProcessScore_ReturnEmptyMessage()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "Q1"
                },

                new ResultDetail
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            ]
        };
        var scores = new List<Score>()
        {
            new()
            {
                CreateDate = DateTime.Now
            }
        };

        var ht = match.ResultDetails.Find(r => r.ResourceId == "01");
        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);
        var results = GetQuarter((0, 1));
        var message = await service.VerifyHalfTimeInValid(match, ht, results, GameType.Bo4);

        Assert.Single(match.ResultDetails);
        Assert.Empty(message);
    }

    [Fact]
    public async Task VerifyHalfTimeInValid_MatchBO4AndHalfTimeScoreInconsistentWithRelatedResult_ReturnTrueWithCorrectMessage()
    {
        var results = GetQuarter((0, 2));
        results[0].HomeValue = results[1].HomeValue = "1";
        results[0].AwayValue = results[1].AwayValue = "1";

        var message = await service.VerifyHalfTimeInValid(new Match(), halfTime, results, GameType.Bo4);

        Assert.Equal("Failed Auto Payout: HT(1-2) due to values inconsistency: Q1(1-1), Q2(1-1)", message);
    }

    [Fact]
    public async Task VerifyHalfTimeInValid_MatchBO2_ReturnFalseAndEmpty()
    {
        var message = await service.VerifyHalfTimeInValid(new Match(), halfTime, [], GameType.Bo2);

        Assert.Empty(message);
    }

    [Fact]
    public void FullTimeDraw_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = service.FullTimeDraw(fullTime);

        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Fact]
    public void FullTimeDraw_FinalNotDraw_ReturnFalseAndEmpty()
    {
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "1";
        var actual = service.FullTimeDraw(fullTime);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task FinalOfBo2InValid_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = await service.FinalOfBo2InValid(new Match(), fullTime, halfTime);

        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Fact]
    public async Task FinalOfBo2InValid_HalfTimeIsNullAndMaxTimeScoreGreaterThanRetryProcessScore_ReturnTrueAndCorrectMessage()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "00"
                },

                new ResultDetail
                {
                    ResourceId = "01"
                }
            ]
        };
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "1";
        var actual = await service.FinalOfBo2InValid(match, fullTime, null);

        Assert.Equal("Failed Auto Payout: FT(2-1) due to related result(s) not calculated: HT", actual);
        Assert.Equal(2, match.ResultDetails.Count);
    }

    [Fact]
    public async Task FinalOfBo2InValid_HalfTimeIsNullAndMaxTimeScoreLessThanRetryProcessScore_ReturnEmptyMessage()
    {
        var scores = new List<Score>()
        {
            new()
            {
                CreateDate = DateTime.Now
            }
        };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "00",
                    HomeValue = "1",
                    AwayValue = "2"
                },

                new ResultDetail
                {
                    ResourceId = "01"
                }
            ]
        };

        var ft = match.ResultDetails.Find(r => r.ResourceId == "00");

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);
        var actual = await service.FinalOfBo2InValid(match, ft, null);

        Assert.Single(match.ResultDetails);
        Assert.Empty(actual);
    }

    [Fact]
    public async Task FinalOfBo2InValid_HalfTimeScoreIsGreaterThanFullTime_ReturnMessage()
    {
        fullTime.HomeValue = "0";
        fullTime.AwayValue = "1";

        var actual = await service.FinalOfBo2InValid(new Match(), fullTime, halfTime);

        Assert.Equal("Failed Auto Payout: FT(0-1) due to values inconsistency: HT(1-2)", actual);
    }

    [Fact]
    public async Task FinalOfBo2InValid_HalfTimeAndFullTimeIsValid_ReturnTrueAndCorrectMessageAndSetHtScoreIntoFullTime()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var scores = new List<Score>()
        {
            new()
            {
                CreateDate = DateTime.Now
            }
        };

        resultDetailService.GetSingleScore(Arg.Any<ResultDetail>()).Returns(scores);

        var actual = await service.FinalOfBo2InValid(new Match(), fullTime, halfTime);

        Assert.Empty(actual);
        Assert.Equal("1-2", fullTime.RelatedInfo);
    }

    [Fact]
    public async Task FinalOfBo4InValid_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, []);

        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Theory]
    [InlineData(0, 1, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q2, HT, Q3, Q4")]
    [InlineData(1, 2, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q1, HT, Q3, Q4")]
    [InlineData(0, 2, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: HT, Q3, Q4")]
    [InlineData(0, 3, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q3, Q4")]
    [InlineData(0, 4, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q4")]
    public async Task FinalOfBo4InValid_RelatedResultInvalidAndMaxTimeScoreGreaterThanRetryProcessScore_ReturnTrueAndCorrectMessage(int from, int to, string errorMessage)
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((from, to));
        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, results);

        Assert.Equal(errorMessage, actual);
    }

    [Fact]
    public async Task FinalOfBo4InValid_ExistOTAndSumScoreNotDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        results[0].HomeValue = "2";
        var otResult = new ResultDetail { ResourceId = ResourceIds.OverTime, HomeValue = "1", AwayValue = "2", RelatedInfo = "SBO:1-1" };

        overTimeService.GetOtBasketBall(fullTime, Arg.Any<int>(), Arg.Any<int>()).Returns(otResult);

        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, results);

        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(2-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1), OT(SBO:1-1)", actual);
    }

    [Fact]
    public async Task FinalOfBo4InValid_NotExistOTAndSumScoreIsDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        var otResult = new ResultDetail { ResourceId = ResourceIds.OverTime, HomeValue = "0", AwayValue = "0" };
        overTimeService.GetOtBasketBall(fullTime, Arg.Any<int>(), Arg.Any<int>()).Returns(otResult);

        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, results);

        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(1-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1)", actual);
    }

    [Fact]
    public async Task FinalOfBo4InValid_ExistOTAndBothSumScoreAndOTScoreAreDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        var otResult = new ResultDetail { ResourceId = ResourceIds.OverTime, HomeValue = "1", AwayValue = "1", RelatedInfo = "SBO:1-1" };

        overTimeService.GetOtBasketBall(fullTime, Arg.Any<int>(), Arg.Any<int>()).Returns(otResult);

        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, results);

        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(1-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1), OT(SBO:1-1)", actual);
    }

    [Fact]
    public async Task FinalOfBo4InValid_TotalScoreNotMatchWithFinalScore_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        results[0].HomeValue = "0";
        var otResult = new ResultDetail { ResourceId = ResourceIds.OverTime, HomeValue = "1", AwayValue = "1", IsValid = false };
        overTimeService.GetOtBasketBall(fullTime, Arg.Any<int>(), Arg.Any<int>()).Returns(otResult);

        var actual = await service.FinalOfBo4InValid(new Match(), fullTime, results);

        Assert.Equal("Failed Auto Payout: FT(2-4) due to values inconsistency: Q1(0-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1)", actual);
    }

    [Fact]
    public async Task FinalOfBo4InValid_TotalScoreScoreEqualFullTimeScoreAndOtValid_ReturnFalseAndEmptyMessage()
    {
        fullTime.HomeValue = "5";
        fullTime.AwayValue = "6";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        var otResult = new ResultDetail { ResourceId = ResourceIds.OverTime, HomeValue = "1", AwayValue = "2" };
        var match = new Match
        {
            ResultDetails = [fullTime]
        };

        overTimeService.GetOtBasketBall(fullTime, Arg.Any<int>(), Arg.Any<int>()).Returns(otResult);
        var actual = await service.FinalOfBo4InValid(match, fullTime, results);

        Assert.Empty(actual);
        Assert.Equal(2, match.ResultDetails.Count);
    }
}