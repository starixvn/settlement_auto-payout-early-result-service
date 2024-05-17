using AutoPayout.Conditions.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class TennisExtraStepTests
{
    private readonly IDynamicRepository repository;

    private readonly TennisInsResourceResult extraStep;

    public TennisExtraStepTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        extraStep = new TennisInsResourceResult(repository);
    }

    [Fact]
    public async Task Process_Always_ExecuteRefundIns()
    {
        await extraStep.Process(new Match() { MatchId = 1, ResourceType = ResourceType.MainResource }, new SettleCriteria());

        await repository.Received().ExecuteAsync(Arg.Any<InsRefundResultCriteria>());
    }
}