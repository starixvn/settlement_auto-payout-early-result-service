using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyTimeResultTests
{
    private readonly ResultDetailService resultDetailService;
    private readonly VerifyTimeResult verifyTimeResult;
    private readonly Match mockMatch;

    public VerifyTimeResultTests()
    {
        resultDetailService = Substitute.For<ResultDetailService>();
        var scoreMessageService = Substitute.For<ScoreMessageService>();
        var appSettingsKey = Substitute.For<IOptions<AppSettingsKey>>();

        verifyTimeResult = new VerifyTimeResult(resultDetailService, scoreMessageService, appSettingsKey);

        appSettingsKey.Value.Returns(new AppSettingsKey { RetryProcessScore = 200 });

        mockMatch = new Match
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
        mockMatch.ResultDetails = mockMatch.ResultDetails.Where(x => x.ResultType != ResultTypes.Time).ToList();

        // Act
        await verifyTimeResult.HandleCondition(mockMatch);

        // Assert
        await resultDetailService.DidNotReceiveWithAnyArgs().GetScores(null);
        Assert.Single(mockMatch.ResultDetails); // Only normal results remain
    }

    [Fact]
    public async Task HandleCondition_ShouldProcess_WhenTimeResultsExist()
    {
        // Arrange
        var scores = new List<Score>
        {
            new Score { ResourceId = "909001", HomeValue = "1", AwayValue = "30", SourceId = SourceId.Trader }
        };
        mockMatch.ResultDetails = mockMatch.ResultDetails.Where(x => x.ResultType == ResultTypes.Time).ToList();
        resultDetailService.GetScores(mockMatch).Returns(scores);

        // Act
        await verifyTimeResult.HandleCondition(mockMatch);

        // Assert
        Assert.Single(mockMatch.ResultDetails); // Valid Time result remains
        Assert.Equal("1", mockMatch.ResultDetails.First().HomeValue);
        Assert.Equal("30", mockMatch.ResultDetails.First().AwayValue);
    }

    [Fact]
    public async Task HandleCondition_ShouldThrowException_WhenNoValidResultsRemain()
    {
        // Arrange
        var scores = new List<Score>
        {
            new()
            {
                ResourceId = "909001",
                HomeValue = "1",
                AwayValue = "100",
                SourceId = SourceId.Trader,
                CreateDate = new(2022,11,11)
            }
        }; // No valid scores

        mockMatch.ResultDetails = mockMatch.ResultDetails.Where(x => x.ResultType == ResultTypes.Time).ToList();

        resultDetailService.GetScores(mockMatch).Returns(scores);

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() => verifyTimeResult.HandleCondition(mockMatch));
    }
}