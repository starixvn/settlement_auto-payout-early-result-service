using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalKabaddiTests
{
    private readonly ResultDetailService resultDetailService;
    private readonly IOptions<AppSettingsKey> options;
    private readonly VerifyFinalKabaddi condition;

    public VerifyFinalKabaddiTests()
    {
        resultDetailService = Substitute.For<ResultDetailService>();
        options = Substitute.For<IOptions<AppSettingsKey>>();
        condition = new VerifyFinalKabaddi(resultDetailService, options);
    }

    [Fact]
    public async Task HandleCondition_WhenChildMatchTypeIsTossWinner_ShouldContinueToNextCondition()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 2 }, // TossWinner
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, IsValid = true }
            }
        };

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenChildMatchTypeIsTeamTotalPoints_ShouldContinueToNextCondition()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 1 }, // TeamTotalPoints
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, IsValid = true }
            }
        };

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenChildMatchTypeIsRaidPoints_ShouldContinueToNextCondition()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 3 }, // RaidPoints
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, IsValid = true }
            }
        };

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenChildMatchTypeIsPlayerRaidPoints_ShouldContinueToNextCondition()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 4 }, // PlayerRaidPoints
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, IsValid = true }
            }
        };

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenFinalResultIsValid_ShouldContinueToNextCondition()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 5 }, // Other type
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, IsValid = true, HomeValue = "25", AwayValue = "20" }
            }
        };

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.DidNotReceive().WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenFinalResultIsInvalid_ShouldHandleFinalResult()
    {
        // Arrange
        var finalResult = new ResultDetail
        {
            ResourceId = ResourceIds.FullTime,
            IsValid = false,
            HomeValue = "25",
            AwayValue = "20"
        };

        var match = new Match
        {
            MatchId = 1,
            MatchDetail = new MatchDetail { ChildMatchType = 5 }, // Other type
            ResultDetails = new List<ResultDetail>
            {
                finalResult,
                new() { ResourceId = "HT", IsValid = true, HomeValue = "12", AwayValue = "10" }
            }
        };

        var expectedErrorMessage = ErrorMessage.InvalidInput.Replace("@score", "25-20");

        // Act
        await condition.HandleCondition(match);

        // Assert
        await resultDetailService.Received().UpdateInvalidResultStatus(
            match.MatchId,
            Arg.Is<IList<ResultDetail>>(list => list.Count == 1 && list[0] == finalResult),
            expectedErrorMessage);

        resultDetailService.Received().WriteLogResult(match.MatchId, finalResult, expectedErrorMessage);

        // Final result should be removed from match.ResultDetails
        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == ResourceIds.FullTime);
        Assert.Contains(match.ResultDetails, r => r.ResourceId == "HT");
    }
}