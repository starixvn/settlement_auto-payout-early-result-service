using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers;

public class VerifyFinalResultHelperServiceTests
{
    [Theory]
    [InlineData("1", "2", "1", "3", true)]
    [InlineData("2", "1", "1", "3", false)]
    public void VerifyResult_FinalAndSetInConsistent_ReturnFalse(string firstHome, string firstAway, string secondHome, string secondAway, bool expected)
    {
        var resultDetails = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = firstHome,
                AwayValue = firstAway
            },
            new ()
            {
                ResourceId = "02",
                HomeValue = secondHome,
                AwayValue = secondAway
            }
        };

        var final = new ResultDetail()
        {
            ResourceId = "00",
            HomeValue = "0",
            AwayValue = "2"
        };

        var actual = VerifyFinalResultHelper.VerifyResult(resultDetails, final);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("01,02,03", true)]
    [InlineData("01,03", false)]
    public void IsMatchResultsConsecutive_Always_ReturnBySetPosition(string resourceIds, bool expected)
    {
        var resultDetails = resourceIds.Split(",").Select(r => new ResultDetail() { ResourceId = r });

        var actual = VerifyFinalResultHelper.IsMatchResultsConsecutive(resultDetails);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("1", "2", 3, true)]
    [InlineData("1", "4", 3, false)]
    [InlineData("1", "1", 3, false)]
    [InlineData("2", "1", 5, false)]
    [InlineData("2", "3", 5, true)]
    [InlineData("2", "3", 7, false)]
    [InlineData("4", "3", 7, true)]
    public void FinalScoreIsValid_Always_ReturnByGameSession(string home, string away, int gameSession, bool expected)
    {
        var final = new ResultDetail()
        {
            HomeValue = home,
            AwayValue = away
        };
        var matchDetail = new MatchDetail()
        {
            GameSession = gameSession
        };

        var actual = VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, final);

        Assert.Equal(expected, actual);
    }
}