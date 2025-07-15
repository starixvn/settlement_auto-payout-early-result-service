using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class MatchPoolServiceTests
{
    private readonly MatchService matchService;
    private readonly MatchPoolServiceImp matchPoolService;

    public MatchPoolServiceTests()
    {
        matchService = Substitute.For<MatchService>();
        matchPoolService = new MatchPoolServiceImp(matchService);
    }

    [Fact]
    public async Task GetNewMatches_ShouldReturnNewValidMatches()
    {
        // Arrange
        var existingMatches = new List<Match>
        {
            new Match { MatchId = 1}
        };
        var newMatches = new List<Match>
        {
            new Match { MatchId = 2}
        };
        var newValidMatches = new List<Match>
        {
            new Match { MatchId = 2 }
        };

        matchService.GetMatches().Returns(existingMatches.Concat(newMatches).ToList());
        matchService.GetMatchDetails(Arg.Any<List<Match>>()).Returns(newValidMatches);

        // Act
        var result = await matchPoolService.GetNewMatches();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].MatchId);
    }

    [Fact]
    public async Task GetNewMatches_ShouldAddNewMatchesToPool()
    {
        // Arrange
        var existingMatches = new List<Match>
        {
            new Match { MatchId = 1}
        };
        var newMatches = new List<Match>
        {
            new Match { MatchId = 2}
        };
        var newValidMatches = new List<Match>
        {
            new Match { MatchId = 2 }
        };

        matchPoolService.PoolOfMatches.AddRange(existingMatches);
        matchService.GetMatches().Returns(existingMatches.Concat(newMatches).ToList());
        matchService.GetMatchDetails(Arg.Any<List<Match>>()).Returns(newValidMatches);

        // Act
        await matchPoolService.GetNewMatches();

        // Assert

        Assert.Equal(2, matchPoolService.PoolOfMatches.Count);
        Assert.Equal(2, matchPoolService.PoolOfMatches[1].MatchId);
    }

    [Fact]
    public void RemoveMatch_ShouldRemoveMatchFromPool()
    {
        // Arrange
        var matchToRemove = new Match { MatchId = 1, ResourceType = 1 };
        matchPoolService.PoolOfMatches.Add(matchToRemove);

        // Act
        matchPoolService.RemoveMatch(matchToRemove);

        // Assert
        Assert.Empty(matchPoolService.PoolOfMatches);
    }
}