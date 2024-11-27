using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyResultWithResourceIdTests
{
    private readonly IDynamicRepository _repository;
    private readonly ResultDetailService _resultDetailService;
    private readonly VerifyResultWithResourceId _handler;

    public VerifyResultWithResourceIdTests()
    {
        _repository = Substitute.For<IDynamicRepository>();
        _resultDetailService = Substitute.For<ResultDetailService>();
        _handler = new VerifyResultWithResourceId(_resultDetailService, _repository);
    }

    [Fact]
    public async Task HandleCondition_NoSpecialResources_NoActionTaken()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail { ResourceId = "0102", Scores = [new Score { RelatedCalResources = "0101"}] }
            }
        };

        // Act
        await _handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
        await _resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_ValidRaceToPoint_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "10",
                    AwayValue = "5",
                    Scores = new List<Score> { new Score() }
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }

    [Fact]
    public async Task HandleCondition_ValidRaceToPointNoneValue_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "N",
                    AwayValue = "N",
                    Scores = new List<Score> { new Score() }
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

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
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "N",
                    AwayValue = "5",
                    Scores = new List<Score> { new Score() },
                    IsValid = false
                },
                new ResultDetail
                {
                    ResourceId = "0101",
                    HomeValue = "10",
                    AwayValue = "5",
                    Scores = new List<Score> { new Score() },
                    IsValid = true
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

        // Assert
        await _resultDetailService.Received(1)
            .UpdateInvalidResultStatus(Arg.Is(match.MatchId), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        await _repository.Received(1).ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }
    [Fact]
    public async Task HandleCondition_InValidRaceToPointValue_AreHandledAndLogged()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39030510",
                    HomeValue = "21",
                    AwayValue = "5",
                    Scores = new List<Score> { new Score() },
                    IsValid = false
                },
                new ResultDetail
                {
                    ResourceId = "0101",
                    HomeValue = "10",
                    AwayValue = "5",
                    Scores = new List<Score> { new Score() },
                    IsValid = true
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

        // Assert
        await _resultDetailService.Received(1)
            .UpdateInvalidResultStatus(Arg.Is(match.MatchId), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        await _repository.Received(1).ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }

    [Fact]
    public async Task HandleCondition_ValidPointsPlayed_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39150115",
                    HomeValue = "8",
                    AwayValue = "7",
                    Scores = new List<Score> { new Score() }
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }


    [Fact]
    public async Task HandleCondition_ValidPointsPlayedNoneValue_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "39150115",
                    HomeValue = "N",
                    AwayValue = "N",
                    Scores = new List<Score> { new Score() }
                }
            }
        };

        // Act
        await _handler.HandleCondition(match);

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
            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail { IsValid = false }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() => _handler.HandleCondition(match));
    }
}
