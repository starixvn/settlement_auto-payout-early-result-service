using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers;

public class MuayThaiHelperServiceTests
{
    private readonly MuayThaiHelperServiceImp service = new();

    [Theory]
    [InlineData(1, 0, "1", "0", "1", true)]
    [InlineData(2, 0, "1", "0", "2", true)]
    [InlineData(3, 0, "1", "0", "3", true)]
    [InlineData(4, 0, "1", "0", "4", true)]
    [InlineData(5, 0, "1", "0", "5", true)]
    public void MapSboToSettle_HomeValue1To5AndAwayValue0_ReturnsCorrectMapping(int homeValue, int awayValue, string expectedHome, string expectedAway, string expectedRound, bool expectedCorrectScore)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal(expectedHome, result.Home);
        Assert.Equal(expectedAway, result.Away);
        Assert.Equal(expectedRound, result.Round);
        Assert.Equal(expectedCorrectScore, result.correctScore);
    }

    [Theory]
    [InlineData(0, 1, "0", "1", "1", true)]
    [InlineData(0, 2, "0", "1", "2", true)]
    [InlineData(0, 3, "0", "1", "3", true)]
    [InlineData(0, 4, "0", "1", "4", true)]
    [InlineData(0, 5, "0", "1", "5", true)]
    public void MapSboToSettle_HomeValue0AndAwayValue1To5_ReturnsCorrectMapping(int homeValue, int awayValue, string expectedHome, string expectedAway, string expectedRound, bool expectedCorrectScore)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal(expectedHome, result.Home);
        Assert.Equal(expectedAway, result.Away);
        Assert.Equal(expectedRound, result.Round);
        Assert.Equal(expectedCorrectScore, result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_HomeValue6AndAwayValue0_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(6, 0);

        Assert.Equal("5", result.Home);
        Assert.Equal("0", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_HomeValue0AndAwayValue6_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(0, 6);

        Assert.Equal("0", result.Home);
        Assert.Equal("5", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_HomeValue6AndAwayValue6_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(6, 6);

        Assert.Equal("5", result.Home);
        Assert.Equal("5", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Theory]
    [InlineData(0, 0, "0", "0", "", true)]
    [InlineData(7, 0, "7", "0", "", true)]
    [InlineData(0, 7, "0", "7", "", true)]
    [InlineData(10, 15, "10", "15", "", true)]
    [InlineData(1, 1, "1", "1", "", true)]
    [InlineData(5, 3, "5", "3", "", true)]
    public void MapSboToSettle_OtherValues_ReturnsDefaultMapping(int homeValue, int awayValue, string expectedHome, string expectedAway, string expectedRound, bool expectedCorrectScore)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal(expectedHome, result.Home);
        Assert.Equal(expectedAway, result.Away);
        Assert.Equal(expectedRound, result.Round);
        Assert.Equal(expectedCorrectScore, result.correctScore);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-5, -3)]
    public void MapSboToSettle_NegativeValues_ReturnsDefaultMapping(int homeValue, int awayValue)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal(homeValue.ToString(), result.Home);
        Assert.Equal(awayValue.ToString(), result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    public void MapSboToSettle_HomeWins1To5Rounds_ReturnsHomeWinMapping(int homeValue, int awayValue)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal("1", result.Home);
        Assert.Equal("0", result.Away);
        Assert.Equal(homeValue.ToString(), result.Round);
        Assert.True(result.correctScore);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(0, 3)]
    [InlineData(0, 4)]
    [InlineData(0, 5)]
    public void MapSboToSettle_AwayWins1To5Rounds_ReturnsAwayWinMapping(int homeValue, int awayValue)
    {
        var result = service.MapSboToSettle(homeValue, awayValue);

        Assert.Equal("0", result.Home);
        Assert.Equal("1", result.Away);
        Assert.Equal(awayValue.ToString(), result.Round);
        Assert.True(result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_HomeWinsByKO_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(6, 0);

        Assert.Equal("5", result.Home);
        Assert.Equal("0", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_AwayWinsByKO_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(0, 6);

        Assert.Equal("0", result.Home);
        Assert.Equal("5", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }

    [Fact]
    public void MapSboToSettle_DrawByKO_ReturnsCorrectMapping()
    {
        var result = service.MapSboToSettle(6, 6);

        Assert.Equal("5", result.Home);
        Assert.Equal("5", result.Away);
        Assert.Equal(string.Empty, result.Round);
        Assert.True(result.correctScore);
    }
}