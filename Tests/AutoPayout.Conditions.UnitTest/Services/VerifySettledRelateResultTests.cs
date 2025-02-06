using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifySettledRelateResultTests
{
    private readonly IDynamicRepository repository;
    private readonly VerifySettledRelateResult handler;

    public VerifySettledRelateResultTests()
    {
        repository = Substitute.For<IDynamicRepository>();

        handler = new VerifySettledRelateResult(repository);
    }

    [Fact]
    public async Task HandleCondition_ValidRelatedResults_ResultsMarkedValid()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,

            ResultDetails = new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "Resource1",
                    HomeValue = "1",
                    AwayValue = "2",
                },
                new ResultDetail
                {
                    ResourceId = "Resource2",
                    HomeValue = "1",
                    AwayValue = "2",
                }
            }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(new List<ResultDetail>
            {
                new ResultDetail
                {
                    ResourceId = "CalResource1",
                    SettlementStatusId = SettlementStatusId.Calculated
                },
                new ResultDetail
                {
                    ResourceId = "SettResource1",
                    ResultStatusId = ResultStatusId.ManualCompleted
                }
            }));

        // Act
        await handler.HandleCondition(match);

        // Assert
        Assert.All(match.ResultDetails, x => Assert.True(x.IsValid));
    }
}