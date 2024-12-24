using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class AutoServiceTests
{
    [Fact]
    public async Task ProcessAsync_HasMatches_InvokeProcessMethod()
    {
        var matchPoolService = Substitute.For<MatchPoolService>();
        var settleController = Substitute.For<ProcessService>();
        var controller = new AutoService(settleController, matchPoolService);

        // Arrange
        var matches = new List<Match>
        {
            new() { MatchId = 123, ResultDetails = new List<ResultDetail>()}
        };

        matchPoolService.GetNewMatches().Returns(matches);

        // Act
        await controller.Handle();

        // Assert
        await settleController.Received().Process(matches[0]);
    }
}
