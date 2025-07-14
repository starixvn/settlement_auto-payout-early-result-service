using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Models;
using System.Reflection;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalKabaddiTests
{
    [Theory]
    [InlineData("1", "0", true)]
    [InlineData("0", "1", true)]
    [InlineData("0", "0", false)]
    [InlineData("1", "1", false)]
    public void ValidateTossWinner_SetsIsValidCorrectly(string home, string away, bool expected)
    {
        // Arrange
        var result = new ResultDetail { HomeValue = home, AwayValue = away };
        var method = typeof(VerifyFinalKabaddi).GetMethod("ValidateTossWinner", BindingFlags.Static | BindingFlags.NonPublic);

        // Act
        method.Invoke(null, [result, int.Parse(home), int.Parse(away)]);

        // Assert
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(18486, 123, 0, 0, true)]
    [InlineData(18486, 123, 1, 0, false)]
    [InlineData(123, 18486, 0, 0, true)]
    [InlineData(123, 18486, 0, 1, false)]
    [InlineData(18486, 18486, 0, 0, true)]
    [InlineData(123, 456, 0, 0, false)]
    public void ValidateTeamTotalOrRaidPoints_SetsIsValidCorrectly(int homeId, int awayId, int homeScore, int awayScore, bool expected)
    {
        // Arrange
        var matchDetail = new MatchDetail { HomeId = homeId, AwayId = awayId };
        var result = new ResultDetail();
        var method = typeof(VerifyFinalKabaddi).GetMethod("ValidateTeamTotalOrRaidPoints", BindingFlags.Static | BindingFlags.NonPublic);

        // Act
        method.Invoke(null, [matchDetail, result, homeScore, awayScore]);

        // Assert
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(2, false)]
    [InlineData(5, false)]
    public void IsTeamTotalOrRaidPoints_ReturnsExpected(short childMatchType, bool expected)
    {
        // Arrange
        var method = typeof(VerifyFinalKabaddi).GetMethod("IsTeamTotalOrRaidPoints", BindingFlags.Static | BindingFlags.NonPublic);

        // Act
        var result = (bool)method.Invoke(null, new object[] { childMatchType });

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10", "5", 10, 5)]
    [InlineData("abc", "xyz", 0, 0)]
    [InlineData(null, null, 0, 0)]
    public void ParseScores_ParsesCorrectly(string home, string away, int expectedHome, int expectedAway)
    {
        // Arrange
        var result = new ResultDetail { HomeValue = home, AwayValue = away };
        var method = typeof(VerifyFinalKabaddi).GetMethod("ParseScores", BindingFlags.Static | BindingFlags.NonPublic);

        // Act
        var tuple = ((int homeScore, int awayScore))method.Invoke(null, new object[] { result });

        // Assert
        Assert.Equal(expectedHome, tuple.homeScore);
        Assert.Equal(expectedAway, tuple.awayScore);
    }
}