using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyResultWithResourceIdTests
{
    private readonly IDynamicRepository repository;
    private readonly ResultDetailService resultDetailService;
    private readonly VerifyResultWithResourceId handler;

    public VerifyResultWithResourceIdTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        handler = new VerifyResultWithResourceId(resultDetailService, repository);
    }

    [Fact]
    public async Task HandleCondition_NoSpecialResources_NoActionTaken()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails =
                [new ResultDetail { ResourceId = "0102" }]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_ValidRaceToPoint_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "10",
                    AwayValue = "5",

                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }

    [Fact]
    public async Task HandleCondition_ValidRaceToPointNoneValue_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "N",
                    AwayValue = "N",

                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }

    [Fact]
    public async Task HandleCondition_InValidRaceToPointNoneValue_AreHandledAndLogged()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "N",
                    AwayValue = "5",

                    IsValid = false
                },

                new ResultDetail
                {
                    ResourceId = "0101",
                    HomeValue = "10",
                    AwayValue = "5",

                    IsValid = true
                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        await resultDetailService.Received(1)
            .UpdateInvalidResultStatus(Arg.Is(match.MatchId), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        await repository.Received(1).ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }
    [Fact]
    public async Task HandleCondition_InValidRaceToPointValue_AreHandledAndLogged()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "21",
                    AwayValue = "5",
                    IsValid = false
                },

                new ResultDetail
                {
                    ResourceId = "0101",
                    HomeValue = "10",
                    AwayValue = "5",
                    IsValid = true
                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        await resultDetailService.Received(1)
            .UpdateInvalidResultStatus(Arg.Is(match.MatchId), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        await repository.Received(1).ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }

    [Fact]
    public async Task HandleCondition_ValidPointsPlayed_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39150115",
                    HomeValue = "8",
                    AwayValue = "7",
                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }


    [Fact]
    public async Task HandleCondition_ValidPointsPlayedNoneValue_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "39150115",
                    HomeValue = "N",
                    AwayValue = "N",
                }
            ]
        };

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }


    [Fact]
    public async Task HandleCondition_NoValidResults_ThrowsServiceException()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = [new ResultDetail { IsValid = false }]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() => handler.HandleCondition(match));
    }
}
