using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyConsistencePlayerResultTests
{
    private readonly ResultDetailService _resultDetailService;
    private readonly VerifyConsistencePlayerResult _condition;

    public VerifyConsistencePlayerResultTests()
    {
        _resultDetailService = Substitute.For<ResultDetailService>();
        _condition = new VerifyConsistencePlayerResult(_resultDetailService);
    }

    [Fact]
    public async Task HandleCondition_NoPlayerResults_CallsNextCondition()
    {
        // Arrange
        var match = CreateMatch(new List<ResultDetail>
        {
            new() { ResourceId = "00" }, // Non-player result
            new() { ResourceId = "01" }  // Non-player result
        });

        // Act
        await _condition.HandleCondition(match);

        // Assert
        await _resultDetailService.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        Assert.Equal(2, match.ResultDetails.Count);
    }

    [Fact]
    public async Task HandleCondition_PlayerResultsExist_FiltersAndValidates()
    {
        // Arrange
        var playerResult = CreatePlayerResult("65301", "Y", "10");
        var nonPlayerResult = new ResultDetail { ResourceId = "00", IsValid = true };

        var match = CreateMatch(new List<ResultDetail> { playerResult, nonPlayerResult });

        // Act
        await _condition.HandleCondition(match);

        // Assert
        Assert.Contains(nonPlayerResult, match.ResultDetails);
        Assert.Contains(playerResult, match.ResultDetails);
    }

    [Fact]
    public async Task HandleCondition_ValidPlayerResult_KeepsResult()
    {
        // Arrange
        var playerResult = CreateValidPlayerResult();
        var match = CreateMatch(new List<ResultDetail> { playerResult });

        // Act
        await _condition.HandleCondition(match);

        // Assert
        Assert.Single(match.ResultDetails);
        Assert.True(match.ResultDetails.First().IsValid);
        await _resultDetailService.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
    }

    // Helper Methods
    private static Match CreateMatch(List<ResultDetail> resultDetails)
    {
        return new Match
        {
            MatchId = 12345,
            ResultDetails = resultDetails
        };
    }

    private static ResultDetail CreatePlayerResult(string resourceId, string homeValue, string relatedHomeValue)
    {
        return new ResultDetail
        {
            ResourceId = resourceId,
            HomeValue = homeValue,
            IsValid = true,
            OddFlex = new List<OddFlexModel>
            {
                new()
                {
                    ResourceId = resourceId,
                    Info = new OddsInfoModel { PlayerId = 123, TeamId = 456 }
                }
            },
            RelatedResults = new List<ResultDetail>
            {
                new() { HomeValue = relatedHomeValue }
            },
            Scores = new List<Score>()
        };
    }

    private static ResultDetail CreateValidPlayerResult()
    {
        var result = CreatePlayerResult("65301", "10", "10");
        result.IsValid = true;
        return result;
    }
}