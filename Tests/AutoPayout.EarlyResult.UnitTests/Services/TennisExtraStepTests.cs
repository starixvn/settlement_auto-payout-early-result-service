using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.Models;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class TennisExtraStepTests
{
    private readonly IDynamicRepository repository;

    private readonly TennisExtraStep extraStep;

    public TennisExtraStepTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        extraStep = new TennisExtraStep(repository);
    }

    [Fact]
    public async Task Process_Always_ExecuteRefundIns()
    {
        await extraStep.Process(new Match() { MatchId = 1, ResourceType = ResourceType.MainResource });

        await repository.Received().ExecuteAsync(Arg.Any<InsRefundResultCriteria>());
    }
}