using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Exceptions;
using AutoPayout.Conditions.Models;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTest.Services;

public class VerifyMatchStatusTests
{
    private readonly IDynamicRepository repository;

    private readonly VerifyMatchStatus condition;

    public VerifyMatchStatusTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        condition = new VerifyMatchStatus(repository);
    }

    [Fact]
    public async Task HandleCondition_NullMatchStatus_ThrowException()
    {
        repository.FetchMultipleAsync<MatchRemark, object>(Arg.Any<MatchRemarkCriteria>()).Returns((new List<MatchRemark>(), null));

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(new Match()));

        Assert.IsType<HoldStatusException>(exception);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(64)]
    public async Task HandleCondition_MatchOnHold_ThrowException(byte settlementStatus)
    {
        repository.FetchMultipleAsync<MatchRemark, object>(Arg.Any<MatchRemarkCriteria>())
            .Returns((new List<MatchRemark> { new() { SettlementStatus = settlementStatus } }, null));

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(new Match()));

        Assert.IsType<HoldStatusException>(exception);
    }

    [Fact]
    public async Task HandleCondition_MatchNotRunning_ThrowException()
    {
        repository.FetchMultipleAsync<MatchRemark, object>(Arg.Any<MatchRemarkCriteria>())
            .Returns((new List<MatchRemark> { new() { SettlementStatus = 0, EventStatus = "Completed" } }, null));

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(new Match()));

        Assert.IsType<InvalidMatchStatusException>(exception);
    }

    [Fact]
    public async Task HandleCondition_MatchRunningAndNotOnHod_DoNotThrowException()
    {
        repository.FetchMultipleAsync<MatchRemark, object>(Arg.Any<MatchRemarkCriteria>())
            .Returns((new List<MatchRemark> { new() { SettlementStatus = 0, EventStatus = EventStatus.Running } }, null));

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(new Match()));

        Assert.Null(exception);
    }
}