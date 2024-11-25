using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers.GetAndGenerate;
public class CreateMessageServiceTests
{
    [Theory]
    [InlineData(ResourceIds.FullTime, 1, "Failed Auto Payout: FT(1-2) due to invalid match type: GameSession(1)")]
    [InlineData(ResourceIds.Halftime, 2, "Failed Auto Payout: HT(1-2) due to invalid match type: GameSession(2)")]
    [InlineData(ResourceIds.OverTime, 3, "Failed Auto Payout: OT(1-2) due to invalid match type: GameSession(3)")]
    public void InconsistentWithGameSession_ValidInputs_ReturnsExpectedMessage(string resourceId, int gameSession, string expected)
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = resourceId,
            HomeValue = "1",
            AwayValue = "2"
        };

        // Act
        var actual = CreateMessageService.InconsistentWithGameSession(resultDetail, gameSession);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ResultsInconsistentWithGameSession_ValidInputs_ReturnsExpectedMessage()
    {
        // Arrange
        var resultDetails = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "Q1",
                HomeValue = "1",
                AwayValue = "2"
            },
            new ()
            {
                ResourceId = "Q2",
                HomeValue = "1",
                AwayValue = "3"
            },
        };

        // Act
        var actual = CreateMessageService.ResultsInconsistentWithGameSession(resultDetails, GameType.Bo1);

        // Assert
        Assert.Equal("Failed Auto Payout: Q1(1-2), Q2(1-3) due to invalid match type: GameSession(1)", actual);
    }

    [Theory]
    [InlineData(ResourceIds.FullTime, "Failed Auto Payout: FT(1-2) due to values inconsistency: Q1(1-2), Q2(1-3)")]
    [InlineData(ResourceIds.Halftime, "Failed Auto Payout: HT(1-2) due to values inconsistency: Q1(1-2), Q2(1-3)")]
    public void ResultRelatedInconsistent_ValidInputs_ReturnsExpectedMessage(string resourceId, string expected)
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = resourceId,
            HomeValue = "1",
            AwayValue = "2"
        };
        var resultDetails = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "Q1",
                HomeValue = "1",
                AwayValue = "2"
            },
            new ()
            {
                ResourceId = "Q2",
                HomeValue = "1",
                AwayValue = "3"
            },
        };

        // Act
        var actual = CreateMessageService.ResultRelatedInconsistent(resultDetail, resultDetails, ResourceIds.OverTime);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void HalfTimeNotCalculated_Always_ReturnsExpectedMessage()
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = ResourceIds.FullTime,
            HomeValue = "1",
            AwayValue = "2"
        };

        // Act
        var actual = CreateMessageService.HalfTimeNotCalculated(resultDetail);

        // Assert
        Assert.Equal("Failed Auto Payout: FT(1-2) due to related result(s) not calculated: HT", actual);
    }

    [Fact]
    public void HalfTimeInconsistentWithFT_Always_ReturnsExpectedMessage()
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = ResourceIds.FullTime,
            HomeValue = "1",
            AwayValue = "2"
        };

        var halfTime = new ResultDetail
        {
            ResourceId = ResourceIds.Halftime,
            HomeValue = "1",
            AwayValue = "1"
        };

        // Act
        var actual = CreateMessageService.HalfTimeInconsistentWithFT(resultDetail, halfTime);

        // Assert
        Assert.Equal("Failed Auto Payout: FT(1-2) due to values inconsistency: HT(1-1)", actual);
    }

    [Theory]
    [InlineData(ResourceIds.FullTime, "Failed Auto Payout: FT(1-2) due to related result(s) not calculated: Q1, Q2, HT, Q3, Q4")]
    [InlineData(ResourceIds.Halftime, "Failed Auto Payout: HT(1-2) due to related result(s) not calculated: Q1, Q2")]
    public void RelatedResultInvalid_ValidInputs_ReturnsExpectedMessage(string resourceId, string expected)
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = resourceId,
            HomeValue = "1",
            AwayValue = "2"
        };
        var listResourceNeeded = resourceId == ResourceIds.Halftime
            ? ResourceIds.ResultHtBasketBall
            : ResourceIds.ResultFtWithoutOtBasketBall;
        // Act
        var actual = CreateMessageService.RelatedResultInvalid(resultDetail, Array.Empty<string>(), listResourceNeeded);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FullTimeDrawMessage_Always_ReturnsExpectedMessage()
    {
        // Arrange
        var resultDetail = new ResultDetail
        {
            ResourceId = ResourceIds.FullTime,
            HomeValue = "1",
            AwayValue = "1"
        };


        // Act
        var actual = CreateMessageService.FullTimeDrawMessage(resultDetail);

        // Assert
        Assert.Equal("Failed Auto Payout: FT(1-1) due to FT should not be tie.", actual);
    }

}

