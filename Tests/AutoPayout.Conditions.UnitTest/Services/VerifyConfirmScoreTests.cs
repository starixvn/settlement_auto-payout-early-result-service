using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyConfirmScoreTests
{
    private readonly ResultDetailService resultDetailService;
    private readonly ScoreMessageService scoreMessageService;
    private readonly VerifyConfirmScore condition;

    public VerifyConfirmScoreTests()
    {
        resultDetailService = Substitute.For<ResultDetailService>();
        scoreMessageService = Substitute.For<ScoreMessageService>();
        var appSetting = new AppSettingsKey { RetryProcessScore = 300000 };
        var option = Options.Create(appSetting);
        condition = new VerifyConfirmScore(resultDetailService, scoreMessageService, option);
    }

    [Fact]
    public async Task HandleCondition_ScoresAreEqual_AttachValueToResult()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail, secondResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100,
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 200,
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200,
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Equal("1", firstResultDetail.HomeValue);
        Assert.Equal("2", firstResultDetail.AwayValue);
        Assert.Equal(2, firstResultDetail.ExternalSourceInfo.Count());
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("2", secondResultDetail.AwayValue);
        Assert.Single(secondResultDetail.ExternalSourceInfo);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => !r.Any()), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_AwayNullAndBlank_ComparePassed()
    {
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [secondResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = string.Empty,
                SourceId = 200,
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = null,
                SourceId = 200,
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Empty(secondResultDetail.AwayValue);
        Assert.Equal(2, secondResultDetail.ExternalSourceInfo.Count());
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => !r.Any()), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_ScoreNotEqualAndTimeOverCurrentTime_MarkFail()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-7),
                  },
            new()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => r.Count() == 1), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_TwoProviderDifferentTimes_CheckWithMaxDate()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-7)
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-2)
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => !r.Any()), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_SomeEqualAndSomeNot_AttachValueToResultForResultConfirmOnly()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail, secondResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100,
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Null(firstResultDetail.HomeValue);
        Assert.Null(firstResultDetail.AwayValue);
        Assert.Empty(firstResultDetail.ExternalSourceInfo);
        Assert.False(firstResultDetail.IsValid);
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("2", secondResultDetail.AwayValue);
        Assert.Single(secondResultDetail.ExternalSourceInfo);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => r.Count() == 1), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_AllScoresNotEqual_ThrowException()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail, secondResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            }, new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
        };
        const string errorMessage = "Failed Auto Payout: Result(01,02) values inconsistent between ";

        scoreMessageService.GetScoreMessage(Arg.Any<IEnumerable<Score>>(), Arg.Any<string>()).Returns(errorMessage);
        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        Assert.False(((ServiceException)exception).MarkFail);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => r.Count() == 2), errorMessage);
    }

    [Fact]
    public async Task HandleCondition_IgnoreCase_PassCompareScore()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail, secondResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "A",
                SourceId = 100
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "a",
                SourceId = 200
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "3",
                SourceId = 200,
            }, new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "3",
                SourceId = 200,
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Equal("A", firstResultDetail.HomeValue);
        Assert.Equal(string.Empty, firstResultDetail.AwayValue);
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("3", secondResultDetail.AwayValue);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>());
    }

    [Fact]
    public async Task HandleCondition_AwayValueNotValid_MarkFail()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [firstResultDetail]
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-7),
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 200,
                CreateDate = DateTime.Now.AddMinutes(-6),
            },
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Is<IList<ResultDetail>>(r => r.Count() == 1), Arg.Any<string>());
    }
}