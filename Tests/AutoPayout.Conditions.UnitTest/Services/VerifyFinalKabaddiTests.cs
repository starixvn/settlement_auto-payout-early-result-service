using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalKabaddiTests
{
    private readonly ResultDetailService resultDetailService = Substitute.For<ResultDetailService>();
    private readonly VerifyFinalKabaddi sut;

    public VerifyFinalKabaddiTests()
    {
        sut = new VerifyFinalKabaddi(resultDetailService);
    }

    [Theory]
    [InlineData("1", "0", true)]
    [InlineData("0", "1", true)]
    public async Task HandleCondition_TossWinner_Valid(string home, string away, bool expected)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = 2 }, ResultDetails = new List<ResultDetail> { finalResult } };

        // Act
        await sut.HandleCondition(match);

        // Assert
        Assert.Equal(expected, finalResult.IsValid);
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("2", "0")]
    [InlineData("0", "2")]
    [InlineData("1", "1")]
    [InlineData("0", "0")]
    public async Task HandleCondition_TossWinner_Invalid(string home, string away)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = 2 }, ResultDetails = new List<ResultDetail> { finalResult } };
        var expectedError = string.Format(ErrorMessage.InvalidInput, $"{home}-{away}");

        // Act
        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0] == finalResult), expectedError);
        resultDetailService.Received(1).WriteLogResult(match.MatchId, finalResult, expectedError);
    }

    [Theory]
    [InlineData(1, "0", "0")]
    [InlineData(3, "0", "0")]
    [InlineData(4, "0", "0")]
    public async Task HandleCondition_TeamTotalOrRaidPoints_Valid(short childType, string home, string away)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = childType, HomeId = 18486, AwayId = 123 }, ResultDetails = new List<ResultDetail> { finalResult } };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData(1, "10", "0")]
    [InlineData(3, "10", "0")]
    [InlineData(4, "10", "0")]
    public async Task HandleCondition_TeamTotalOrRaidPoints_InvalidHome(short childType, string home, string away)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = childType, HomeId = 18486, AwayId = 123 }, ResultDetails = new List<ResultDetail> { finalResult } };
        var expectedError = string.Format(ErrorMessage.InvalidInput, $"{home}-{away}");

        // Act
        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0] == finalResult), expectedError);
        resultDetailService.Received(1).WriteLogResult(match.MatchId, finalResult, expectedError);
        Assert.Empty(match.ResultDetails);
    }

    [Theory]
    [InlineData(1, "0", "10")]
    [InlineData(3, "0", "10")]
    [InlineData(4, "0", "10")]
    public async Task HandleCondition_TeamTotalOrRaidPoints_InvalidAway(short childType, string home, string away)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = childType, HomeId = 123, AwayId = 18486 }, ResultDetails = new List<ResultDetail> { finalResult } };
        var expectedError = string.Format(ErrorMessage.InvalidInput, $"{home}-{away}");

        // Act
        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0] == finalResult), expectedError);
        resultDetailService.Received(1).WriteLogResult(match.MatchId, finalResult, expectedError);
        Assert.Empty(match.ResultDetails);
    }

    [Theory]
    [InlineData(1, "10", "10")]
    [InlineData(3, "10", "10")]
    [InlineData(4, "10", "10")]
    public async Task HandleCondition_TeamTotalOrRaidPoints_NoTeamIdNotAvailable(short childType, string home, string away)
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = home, AwayValue = away };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = childType, HomeId = 123, AwayId = 456 }, ResultDetails = new List<ResultDetail> { finalResult } };
        var expectedError = string.Format(ErrorMessage.InvalidInput, $"{home}-{away}");

        // Act
        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0] == finalResult), expectedError);
        resultDetailService.Received(1).WriteLogResult(match.MatchId, finalResult, expectedError);
    }

    [Fact]
    public async Task HandleCondition_OtherChildMatchType_ShouldNotModifyValidity()
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = "20", AwayValue = "10", IsValid = true };
        var match = new Match { MatchId = 1, MatchDetail = new MatchDetail { ChildMatchType = 5 }, ResultDetails = new List<ResultDetail> { finalResult } };

        // Act
        await sut.HandleCondition(match);

        // Assert
        Assert.True(finalResult.IsValid);
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
        Assert.Single(match.ResultDetails);
    }

    [Fact]
    public async Task HandleCondition_InvalidResult_RemovesFullTimeResult()
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime, HomeValue = "20", AwayValue = "10", IsValid = false };
        var otherResult = new ResultDetail { ResourceId = "HT", HomeValue = "10", AwayValue = "5" };
        var match = new Match { MatchId = 1, ResultDetails = new List<ResultDetail> { finalResult, otherResult }, MatchDetail = new MatchDetail { ChildMatchType = 2 } };
        var expectedError = string.Format(ErrorMessage.InvalidInput, "20-10");

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0] == finalResult), expectedError);
        resultDetailService.Received(1).WriteLogResult(match.MatchId, finalResult, expectedError);
        Assert.Single(match.ResultDetails);
        Assert.Equal("HT", match.ResultDetails[0].ResourceId);
    }
}