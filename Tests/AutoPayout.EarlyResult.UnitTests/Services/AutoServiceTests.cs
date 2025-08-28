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

        var matches = new List<Match>
        {
            new() { MatchId = 123, ResultDetails = [] }
        };

        matchPoolService.GetNewMatches().Returns(matches);

        await controller.Handle();

        await WaitForProcessCall(settleController);

        await settleController.Received().Process(Arg.Any<Match>());
    }

    [Fact]
    public async Task ProcessAsync_NoMatches_DoesNotInvokeProcessMethod()
    {
        var matchPoolService = Substitute.For<MatchPoolService>();
        var settleController = Substitute.For<ProcessService>();
        var controller = new AutoService(settleController, matchPoolService);

        matchPoolService.GetNewMatches().Returns([]);

        await controller.Handle();

        await settleController.DidNotReceive().Process(Arg.Any<Match>());
    }

    private static async Task WaitForProcessCall(ProcessService settleController)
    {
        const int maxAttempts = 10;
        const int delayMs = 3;

        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                await settleController.Received().Process(Arg.Any<Match>());
                return;
            }
            catch (Exception)
            {
                if (i == maxAttempts - 1)
                {
                    throw;
                }
                await Task.Delay(delayMs);
            }
        }
    }
}