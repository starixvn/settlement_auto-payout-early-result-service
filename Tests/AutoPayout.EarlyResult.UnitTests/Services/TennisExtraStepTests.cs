using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
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
        await extraStep.Process(new SettleCriteria() { MatchId = 1 });

        await repository.Received().ExecuteAsync(Arg.Any<InsRefundResultCriteria>());
    }
}