using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;
public class VerifyRunningOddsTests
{
    private readonly IDynamicRepository repository;

    private readonly VerifyRunningOdds condition;

    public VerifyRunningOddsTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        condition = new VerifyRunningOdds(repository);
    }


    [Fact]
    public async Task HandleCondition_WhenErrCodeGreaterThanZero_ThrowsRunningOddsException()
    {
        // Arrange
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
        };

        await repository.ExecuteAsync(Arg.Do<VerifyOddsCriteria>(c => c.ErrCode = 1));

        // Act

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        // Assert
        Assert.IsType<RunningOddsException>(exception);
    }

    [Fact]
    public async Task HandleCondition_WhenErrCodeZero_DoesNotThrowRunningOddsException()
    {
        // Arrange
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
        };

        await repository.ExecuteAsync(Arg.Do<VerifyOddsCriteria>(c => c.ErrCode = 0));

        var exception = await Record.ExceptionAsync(() => condition.HandleCondition(match));

        // Assert
        Assert.Null(exception);
    }
}
