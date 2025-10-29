using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyOddFlexPlayerResultTests
{
    private readonly ResultDetailService resultDetailService = Substitute.For<ResultDetailService>();
    private readonly IDynamicRepository repository = Substitute.For<IDynamicRepository>();
    private readonly VerifyOddFlexPlayerResult sut;

    public VerifyOddFlexPlayerResultTests()
    {
        sut = new VerifyOddFlexPlayerResult(resultDetailService, repository);
    }

    [Fact]
    public async Task HandleCondition_NoPlayerResults_CallsNextConditionOnly()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> {
                new ResultDetail { ResourceId = "999" }
            }
        };

        // Act
        await sut.HandleCondition(match);

        // Assert
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
        Assert.Single(match.ResultDetails);
    }

    [Fact]
    public async Task HandleCondition_AllValidPlayerResults_AddsToResultDetails()
    {
        // Arrange
        var playerId = "653A";
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> {
                new ResultDetail { ResourceId = playerId, Scores = new List<Score> {
                    new Score() { SourceId = 0, OtherInfo = "" }
                } }
            }
        };
        repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>()).Returns(new List<OddFlexModel> {
            new OddFlexModel { ResourceId = playerId, OddsInfo = "{ \"pid\": 1, \"pname\": \"Rivas\", \"tid\": 2, \"ResourceID\": \"01\" }" }
        });

        // Act
        await Record.ExceptionAsync(() => sut.HandleCondition(match));

        // Assert
        Assert.Contains(match.ResultDetails, r => r.ResourceId == playerId);
        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<IList<ResultDetail>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_InvalidPlayerResult_CallsUpdateInvalidResultStatus()
    {
        // Arrange
        var playerId = "653B";
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> {
                new ResultDetail { ResourceId = playerId, Scores = new List<Score> {
                    new Score() { SourceId = 0, OtherInfo = "wrong" }
                } }
            }
        };
        repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>()).Returns(new List<OddFlexModel> {
            new OddFlexModel { ResourceId = playerId,  OddsInfo = "{ \"pid\": 1, \"pname\": \"Rivas\", \"tid\": 2, \"ResourceID\": \"01\" }" }
        });

        // Act
        await Record.ExceptionAsync(() => sut.HandleCondition(match));

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(match.MatchId, Arg.Is<IList<ResultDetail>>(l => l.Count == 1 && l[0].ResourceId == playerId), Arg.Any<string>());
        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == playerId && !r.IsValid);
    }

    [Fact]
    public async Task HandleCondition_ConfirmScoreNull_MarksAsValid()
    {
        // Arrange
        var playerId = "653C";
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> {
                new ResultDetail { ResourceId = playerId, Scores = new List<Score>() }
            }
        };
        repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>()).Returns(new List<OddFlexModel> {
            new OddFlexModel { ResourceId = playerId, OddsInfo = "{ \"pid\": 1, \"pname\": \"Rivas\", \"tid\": 2, \"ResourceID\": \"01\" }" }
        });

        // Act
        await sut.HandleCondition(match);

        // Assert
        Assert.Contains(match.ResultDetails, r => r.ResourceId == playerId && r.IsValid);
    }

    [Fact]
    public async Task HandleCondition_OddFlexMissing_MarksAllAsValid()
    {
        // Arrange
        var playerId = "653D";
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> {
                new ResultDetail { ResourceId = playerId, Scores = new List<Score> {
                    new Score() { SourceId = 0, OtherInfo = "" }
                } }
            }
        };
        repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>()).Returns(new List<OddFlexModel>());

        // Act
        await sut.HandleCondition(match);

        // Assert
        Assert.Contains(match.ResultDetails, r => r.ResourceId == playerId && r.IsValid);
    }
}
