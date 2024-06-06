using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers;
public class VerifyFinalResultBasketballServiceTests
{
    private readonly ResultDetail halfTime = new() { ResourceId = ResourceIds.Halftime, HomeValue = "1", AwayValue = "2" };

    private readonly ResultDetail fullTime = new() { ResourceId = ResourceIds.FullTime };

    private readonly BasketballHelperService service;

    private readonly OverTimeService overTimeService;

    public VerifyFinalResultBasketballServiceTests()
    {
        overTimeService = Substitute.For<OverTimeService>();
        service = new BasketballHelperServiceImp(overTimeService);
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
    public void IsHalfTimeInValid_MatchBO1_ReturnTrueWithCorrectMessage()
    {
        var actual = service.IsHalfTimeInValid(halfTime, new List<ResultDetail>(),
            GameType.Bo1, out var message);


        Assert.Equal("Failed Auto Payout: HT(1-2) due to invalid match type: GameSession(1)", message);
    }


    [Theory]
    [InlineData(0, 1, "Failed Auto Payout: HT(1-2) due to related result(s) not calculated: Q2")]
    [InlineData(1, 2, "Failed Auto Payout: HT(1-2) due to related result(s) not calculated: Q1")]
    public void IsHalfTimeInValid_MatchBO4AndRelatedResultNotValid_ReturnTrueWithCorrectMessage(int from, int to, string errorMessage)
    {
        var results = GetQuarter((from, to));
        var actual = service.IsHalfTimeInValid(halfTime, results,
            GameType.Bo4, out var message);


        Assert.Equal(errorMessage, message);
    }

    [Fact]
    public void IsHalfTimeInValid_MatchBO4AndHalfTimeScoreInconsistentWithRelatedResult_ReturnTrueWithCorrectMessage()
    {
        var results = GetQuarter((0, 2));
        results[0].HomeValue = results[1].HomeValue = "1";
        results[0].AwayValue = results[1].AwayValue = "1";

        var actual = service.IsHalfTimeInValid(halfTime, results,
            GameType.Bo4, out var message);


        Assert.Equal("Failed Auto Payout: HT(1-2) due to values inconsistency: Q1(1-1), Q2(1-1)", message);
    }

    [Fact]
    public void IsHalfTimeInValid_MatchBO4AndHalfTimeConsistentWithRelatedResult_ReturnFalseAndEmpty()
    {
        var results = GetQuarter((0, 2));
        results[0].HomeValue = "1";
        results[1].HomeValue = "0";
        results[0].AwayValue = results[1].AwayValue = "1";

        var actual = service.IsHalfTimeInValid(halfTime, results,
            GameType.Bo4, out var message);

        Assert.False(actual);
        Assert.Empty(message);
    }

    [Fact]
    public void IsHalfTimeInValid_MatchBO2_ReturnFalseAndEmpty()
    {
        var actual = service.IsHalfTimeInValid(halfTime, new List<ResultDetail>(), GameType.Bo2, out var message);

        Assert.False(actual);
        Assert.Empty(message);
    }

    [Fact]
    public void IsFullTimeDraw_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = service.IsFullTimeDraw(fullTime);

        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Fact]
    public void IsFullTimeDraw_FinalNotDraw_ReturnFalseAndEmpty()
    {
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "1";
        var actual = service.IsFullTimeDraw(fullTime);

        Assert.Empty(actual);
    }

    [Fact]
    public void IsFinalOfBo2InValid_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = service.IsFinalOfBo2InValid(fullTime, halfTime);


        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Fact]
    public void IsFinalOfBo2InValid_HalfTimeIsNull_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "1";
        var actual = service.IsFinalOfBo2InValid(fullTime, null);

        Assert.Equal("Failed Auto Payout: FT(2-1) due to related result(s) not calculated: HT", actual);
    }

    [Fact]
    public void IsFinalOfBo2InValid_HalfTimeScoreIsGreaterThanFullTime_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "0";
        fullTime.AwayValue = "1";
        var actual = service.IsFinalOfBo2InValid(fullTime, halfTime);


        Assert.Equal("Failed Auto Payout: FT(0-1) due to values inconsistency: HT(1-2)", actual);
    }


    [Fact]
    public void IsFinalOfBo2InValid_HalfTimeAndFullTimeIsValid_ReturnTrueAndCorrectMessageAndSetHtScoreIntoFullTime()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var actual = service.IsFinalOfBo2InValid(fullTime, halfTime);

        Assert.Empty(actual);
        Assert.Equal("1-2", fullTime.RelatedInfo);
    }

    [Fact]
    public async Task IsFinalOfBo4InValid_FinalDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = fullTime.AwayValue = "1";
        var actual = await service.IsFinalOfBo4InValid(fullTime, new List<ResultDetail>());


        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

    [Theory]
    [InlineData(0, 1, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q2, HT, Q3, Q4")]
    [InlineData(1, 2, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q1, HT, Q3, Q4")]
    [InlineData(0, 2, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: HT, Q3, Q4")]
    [InlineData(0, 3, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q3, Q4")]
    [InlineData(0, 4, "Failed Auto Payout: FT(3-4) due to related result(s) not calculated: Q4")]
    public async Task IsFinalOfBo4InValid_RelatedResultInvalid_ReturnTrueAndCorrectMessage(int from, int to, string errorMessage)
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((from, to));
        var actual = await service.IsFinalOfBo4InValid(fullTime, results);


        Assert.Equal(errorMessage, actual);
    }

    [Fact]
    public async Task IsFinalOfBo4InValid_ExistOTAndSumScoreNotDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        results[0].HomeValue = "2";

        var actual = await service.IsFinalOfBo4InValid(fullTime, results);


        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(2-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1), OT(1-1)", actual);
    }
    [Fact]
    public async Task IsFinalOfBo4InValid_NotExistOTAndSumScoreIsDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");

        var actual = await service.IsFinalOfBo4InValid(fullTime, results);


        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(1-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1)", actual);
    }

    [Fact]
    public async Task IsFinalOfBo4InValid_ExistOTAndBothSumScoreAndOTScoreAreDraw_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "3";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");

        var actual = await service.IsFinalOfBo4InValid(fullTime, results);


        Assert.Equal("Failed Auto Payout: FT(3-4) due to values inconsistency: Q1(1-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1), OT(1-1)", actual);
    }


    [Fact]
    public async Task IsFinalOfBo4InValid_TotalScoreNotMatchWithFinalScore_ReturnTrueAndCorrectMessage()
    {
        fullTime.HomeValue = "2";
        fullTime.AwayValue = "4";
        var results = GetQuarter((0, ResourceIds.ResultFtWithoutOTBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        results[0].HomeValue = "0";

        var actual = await service.IsFinalOfBo4InValid(fullTime, results);


        Assert.Equal("Failed Auto Payout: FT(2-4) due to values inconsistency: Q1(0-1), Q2(1-1), HT(1-1), Q3(1-1), Q4(1-1)", actual);
    }


    [Fact]
    public async Task IsFinalOfBo4InValid_TotalScoreScoreEqualFullTimeScoreAndOtValid_ReturnFalseAndEmptyMessage()
    {
        fullTime.HomeValue = "4";
        fullTime.AwayValue = "5";
        var results = GetQuarter((0, ResourceIds.ResultFtBasketBall.Length));
        results.ForEach(r => r.HomeValue = r.AwayValue = "1");
        results.Last().HomeValue = "0";

        var actual = await service.IsFinalOfBo4InValid(fullTime, results);

        Assert.Empty(actual);
    }
}

