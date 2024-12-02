using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyTimeResultTests
{
    private readonly ResultDetailService _resultDetailService;
    private readonly IOptions<AppSettingsKey> _appSettingsKey;
    private readonly VerifyTimeResult _verifyTimeResult;
    private readonly Match _mockMatch;

    public VerifyTimeResultTests()
    {
        _resultDetailService = Substitute.For<ResultDetailService>();
        _appSettingsKey = Substitute.For<IOptions<AppSettingsKey>>();

        _verifyTimeResult = new VerifyTimeResult(_resultDetailService, _appSettingsKey);

        _appSettingsKey.Value.Returns(new AppSettingsKey { RetryProcessScore = 200 });

        _mockMatch = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail { ResourceId = "909001" }, // time
                new ResultDetail {  ResourceId = "01" }
            ],
            EarlyRetryTime = 300
        };
    }


    [Fact]
    public async Task HandleCondition_ShouldNotProcess_WhenNoTimeResultsExist()
    {
        // Arrange
        _mockMatch.ResultDetails = _mockMatch.ResultDetails.Where(x => x.ResultType != ResultTypes.Time).ToList();

        // Act
        await _verifyTimeResult.HandleCondition(_mockMatch);

        // Assert
        await _resultDetailService.DidNotReceiveWithAnyArgs().GetScores(null);
        Assert.Single(_mockMatch.ResultDetails); // Only normal results remain
    }

    [Fact]
    public async Task HandleCondition_ShouldProcess_WhenTimeResultsExist()
    {
        // Arrange
        var scores = new List<Score>
        {
            new Score { ResourceId = "909001", HomeValue = "1", AwayValue = "30", SourceId = SourceId.Trader }
        };
        _mockMatch.ResultDetails = _mockMatch.ResultDetails.Where(x => x.ResultType == ResultTypes.Time).ToList();
        _resultDetailService.GetScores(_mockMatch).Returns(scores);

        // Act
        await _verifyTimeResult.HandleCondition(_mockMatch);

        // Assert
        Assert.Single(_mockMatch.ResultDetails); // Valid Time result remains
        Assert.Equal("1", _mockMatch.ResultDetails.First().HomeValue);
        Assert.Equal("30", _mockMatch.ResultDetails.First().AwayValue);
    }

    [Fact]
    public async Task HandleCondition_ShouldThrowException_WhenNoValidResultsRemain()
    {
        // Arrange
        var scores = new List<Score>
        {
            new Score { ResourceId = "909001", HomeValue = "1", AwayValue = "100", SourceId = SourceId.Trader, CreateDate = new DateTime(2022,11,11) }
        }; // No valid scores


        _mockMatch.ResultDetails = _mockMatch.ResultDetails.Where(x => x.ResultType == ResultTypes.Time).ToList();

        _resultDetailService.GetScores(_mockMatch).Returns(scores);

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() => _verifyTimeResult.HandleCondition(_mockMatch));
    }

}
