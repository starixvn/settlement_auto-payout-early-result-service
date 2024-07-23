using AutoPayout.Conditions.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.AfterSettleTests;

public class TennisInsResourceResultTests
{
    private readonly IDynamicRepository repository;

    private readonly TennisInsResourceResult extraStep;

    public TennisInsResourceResultTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        extraStep = new TennisInsResourceResult(repository);
    }

    [Fact]
    public async Task Process_Always_ExecuteRefundIns()
    {
        await extraStep.Process(new Match() { MatchId = 1, ResourceType = ResourceType.MainResource, ResultDetails = new List<ResultDetail>() { new() { ResourceId = "0101" } } }, new SettleCriteria());

        await repository.Received().ExecuteAsync(Arg.Is<InsRefundResultCriteria>(c => c.ResourceIds == "0101"));
    }
}