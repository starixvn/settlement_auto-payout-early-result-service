using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalWithBetStatusTests
{
    private readonly ResultDetailService resultDetailServiceSub;
    private readonly VerifyFinalWithBetStatus sut;

    public VerifyFinalWithBetStatusTests()
    {
        resultDetailServiceSub = Substitute.For<ResultDetailService>();
        sut = new VerifyFinalWithBetStatus(resultDetailServiceSub);
    }

    [Fact]
    public async Task HandleCondition_WhenNotMainResource_ShouldNotProcess()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = 4,
            MatchDetail = new MatchDetail(),
            ResultDetails = new List<ResultDetail>()
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenChildMatchTypeNotInList_ShouldNotProcess()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { ParentID = 1, ChildMatchType = 99 },
            ResultDetails = new List<ResultDetail>()
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenBetStatusIsEmpty_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = "" },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "1", AwayValue = "0" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }

    [Fact]
    public async Task HandleCondition_WhenBetStatusLengthLessThanRequired_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = "1-0,2-1" },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "1", AwayValue = "0" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }

    [Fact]
    public async Task HandleCondition_WhenScoresNotMatch_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = "1-0,2-1,3-2" },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "7", AwayValue = "3" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }

    [Fact]
    public async Task HandleCondition_WhenScoresMatch_ShouldNotHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = "1-0,2-1,3-2" },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenParentMatchWithOT_ShouldCheck4Sets()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                LeagueType = 2,
                BetStatus = "1-0,2-1,3-2,4-3"
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "10", AwayValue = "6" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenParentMatchWithOTAndInsufficientSets_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                LeagueType = 2,
                ChildMatchType = 23,
                BetStatus = "1-0,2-1,3-2"
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }
}