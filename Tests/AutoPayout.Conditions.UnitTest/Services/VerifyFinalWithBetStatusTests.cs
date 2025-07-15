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
            ResourceType = ResourceType.EarlyResource,
            MatchDetail = new MatchDetail(),
            ResultDetails = new List<ResultDetail>()
            {
                new() { ResourceId = ResourceIds.Halftime, HomeValue = "6", AwayValue = "3" }
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

    [Theory]
    [InlineData(18)]  // Valid ChildMatchType
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    public async Task HandleCondition_WhenValidChildMatchType_ShouldProcess(short childMatchType)
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                ChildMatchType = childMatchType,
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
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<List<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenInvalidChildMatchTypeWithBetStatus_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                ChildMatchType = 99, // Invalid ChildMatchType
                BetStatus = "1-0,2-1,3-2" // Has BetStatus
            },
            ResultDetails =
            [
                new() { ResourceId = ResourceIds.Halftime, HomeValue = "3", AwayValue = "3" },
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            ]
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
    public async Task HandleCondition_WhenInvalidChildMatchTypeWithoutBetStatus_ShouldNotProcess()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                ChildMatchType = 99, // Invalid ChildMatchType
                BetStatus = "" // Empty BetStatus
            },
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
    public async Task HandleCondition_WhenParentMatchWithOT_ShouldCheckAllSets()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                LeagueType = 2,
                BetStatus = "1-0,2-1,3-2,4-3" // 4 sets
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
    public async Task HandleCondition_WhenRegularMatch_ShouldCheckThreeSets()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 0, // Regular match
                LeagueType = 1,
                BetStatus = "1-0,2-1,3-2,4-3" // 4 sets but should only check first 3
            },
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
    public async Task HandleCondition_WhenScoresDoNotMatch_ShouldHandleInvalidResult()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                BetStatus = "1-0,2-1,3-2" // Sum: 6-3
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "7", AwayValue = "3" },
                new() { ResourceId = ResourceIds.Halftime, HomeValue = "6", AwayValue = "3" }
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