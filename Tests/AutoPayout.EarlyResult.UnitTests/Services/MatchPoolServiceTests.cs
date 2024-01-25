using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class MatchPoolServiceTests
{
    private readonly MatchService matchService;
    private readonly MatchPoolService matchPoolService;

    public MatchPoolServiceTests()
    {
        matchService = Substitute.For<MatchService>();
        matchPoolService = new MatchPoolServiceImp(matchService);
    }

    private List<Match> CreateMatches(params int[] matchIds)
    {
        return matchIds.Select(i => new Match { MatchId = i }).ToList();
    }

    [Fact]
    public async Task FetchNewMatch_Twice_OnlyAddNewMatchToPool()
    {
        matchService.GetMatches().Returns(CreateMatches(1, 3));
        await matchPoolService.GetNewMatches();

        matchService.GetMatches().Returns(CreateMatches(2, 3));
        var listMatch = await matchPoolService.GetNewMatches();

        Assert.Single(listMatch);
    }

    [Fact]
    public async Task RemoveMatch_ExistedMatchId_RemoveMatchFromPool()
    {
        // Arrange
        var matches = CreateMatches(1, 2);
        var serviceMock = new MatchPoolServiceImp(matchService);
        matchService.GetMatches().Returns(matches);

        // Act
        await serviceMock.GetNewMatches();
        serviceMock.RemoveMatch(matches[0]);

        // Assert
        Assert.Single(serviceMock.PoolOfMatches);
    }
}
