using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.AfterSettleTests;
public class ESportsAddResourceRefundTests
{
    private readonly IDynamicRepository repository;
    private readonly ESportsAddResourceRefund extraStep;

    public ESportsAddResourceRefundTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        extraStep = new ESportsAddResourceRefund(repository);
    }

    [Fact]
    public async Task Process_WithNonEmptyResourceRefund_ShouldExecuteAddResourceRefundCriteria()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResourceRefund = "1,2,3"
        };

        var settleCriteria = new SettleCriteria();

        // Act
        await extraStep.Process(match, settleCriteria);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<AddResourceRefundCriteria>(criteria =>
            criteria.MatchId == match.MatchId &&
            criteria.ResourceIds == match.ResourceRefund));
    }

    [Fact]
    public async Task Process_WithEmptyResourceRefund_ShouldNotExecuteAddResourceRefundCriteria()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResourceRefund = ""
        };

        var settleCriteria = new SettleCriteria();

        // Act
        await extraStep.Process(match, settleCriteria);

        // Assert
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<AddResourceRefundCriteria>());
    }
}
