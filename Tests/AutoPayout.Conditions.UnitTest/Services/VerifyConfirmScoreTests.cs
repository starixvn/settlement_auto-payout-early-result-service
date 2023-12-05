using AutoPayout.Conditions.Models;
using AutoPayout.Exceptions;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTest.Services;

public class VerifyConfirmScoreTests
{
    private readonly ScoreService scoreService;

    private readonly VerifyConfirmScore condition;

    public VerifyConfirmScoreTests()
    {
        scoreService = Substitute.For<ScoreService>();
        condition = new VerifyConfirmScore(scoreService);
    }

    [Fact]
    public async void HandleCondition_ScoresAreEqual_AttachValueToResult()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { firstResultDetail, secondResultDetail }
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 200
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200
            },
        };

        scoreService.GetScore(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Equal("1", firstResultDetail.HomeValue);
        Assert.Equal("2", firstResultDetail.AwayValue);
        Assert.Equal(2, firstResultDetail.ExternalSourceInfo.Count());
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("2", secondResultDetail.AwayValue);
        Assert.Single(secondResultDetail.ExternalSourceInfo);
        await scoreService.DidNotReceive().UpdateResultStatus(Arg.Any<int>(), Arg.Any<IEnumerable<ResultDetail>>());
    }

    [Fact]
    public async void HandleCondition_SomeEqualAndSomeNot_AttachValueToResultForResultConfirmOnly()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { firstResultDetail, secondResultDetail }
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200
            },
        };

        scoreService.GetScore(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Null(firstResultDetail.HomeValue);
        Assert.Null(firstResultDetail.AwayValue);
        Assert.Null(firstResultDetail.ExternalSourceInfo);
        Assert.False(firstResultDetail.IsValid);
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("2", secondResultDetail.AwayValue);
        Assert.Single(secondResultDetail.ExternalSourceInfo);
        await scoreService.Received().UpdateResultStatus(1, Arg.Is<IEnumerable<ResultDetail>>(r => r.Count() == 1));
    }

    [Fact]
    public async void HandleCondition_AllScoresNotEqual_ThrowException()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { firstResultDetail, secondResultDetail }
        };

        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 100
            },
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "1",
                SourceId = 200
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "1",
                AwayValue = "2",
                SourceId = 200
            }, new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "2",
                SourceId = 200
            },
        };

        scoreService.GetScore(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.IsType<ServiceException>(exception);
        Assert.False(((ServiceException)exception).MarkFail);
        await scoreService.Received().UpdateResultStatus(1, Arg.Is<IEnumerable<ResultDetail>>(r => r.Count() == 2));
    }

    [Fact]
    public async void HandleCondition_IgnoreCase_PassCompareScore()
    {
        var firstResultDetail = new ResultDetail { ResourceId = "01" };
        var secondResultDetail = new ResultDetail { ResourceId = "02" };
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { firstResultDetail, secondResultDetail }
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
                SourceId = 200
            }, new()
            {
                ResourceId = "02",
                HomeValue = "2",
                AwayValue = "3",
                SourceId = 200
            },
        };

        scoreService.GetScore(match).Returns(scores);

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        Assert.Null(exception);
        Assert.Equal("A", firstResultDetail.HomeValue);
        Assert.Null(firstResultDetail.AwayValue);
        Assert.Equal("2", secondResultDetail.HomeValue);
        Assert.Equal("3", secondResultDetail.AwayValue);
        await scoreService.DidNotReceive().UpdateResultStatus(1, Arg.Any<List<ResultDetail>>());
    }
}