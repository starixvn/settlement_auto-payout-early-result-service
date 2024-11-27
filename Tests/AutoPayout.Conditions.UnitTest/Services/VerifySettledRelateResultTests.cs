using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Criteria;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifySettledRelateResultTests
{
    private readonly IDynamicRepository repository;
    private readonly ResultDetailService resultDetailService;
    private readonly IOptions<AppSettingsKey> options;
    private readonly VerifySettledRelateResult handler;

    public VerifySettledRelateResultTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        options = Substitute.For<IOptions<AppSettingsKey>>();
        options.Value.Returns(new AppSettingsKey { RetryProcessScore = 30000 });

        handler = new VerifySettledRelateResult(repository, resultDetailService, options);
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
                    Scores = new List<Score>
                    {
                        new Score
                        {
                            RelatedCalResources = "CalResource1",
                            RelatedSettResources = "SettResource1",
                            CreateDate = DateTime.Now
                        }
                    }
                },
                new ResultDetail
                {
                    ResourceId = "Resource2",
                    HomeValue = "1",
                    AwayValue = "2",
                    Scores = new List<Score>
                    {
                        new Score
                        {
                            CreateDate = DateTime.Now
                        }
                    }
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

    [Fact]
    public async Task HandleCondition_ValidRelatedResults_NotResultValidThrowException()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,

            ResultDetails =
            [

                new ResultDetail
                {
                    ResourceId = "Resource1",
                    HomeValue = "1",
                    AwayValue = "2",
                    Scores =
                    [
                        new Score
                        {
                            RelatedCalResources = "CalResource1",
                            RelatedSettResources = "SettResource1",
                            CreateDate = DateTime.Now
                        }
                    ]
                },
            ]
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(
            [
                new ResultDetail
                {
                    ResourceId = "CalResource1",
                },
                new ResultDetail
                {
                    ResourceId = "SettResource1",
                }
            ]));

        // Assert
        await Assert.ThrowsAsync<ServiceException>(() => handler.HandleCondition(match));
    }

    [Fact]
    public async Task HandleCondition_ExpiredResults_AreHandledAndLogged()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "1",
                    IsValid = false,
                    Expired = true,
                    Scores = new List<Score>
                    {
                        new Score { RelatedCalResources = "CalResource1", CreateDate = new DateTime(2021,1,11) }
                    }
                },
                new ResultDetail
                {
                    ResourceId = "2",
                    IsValid = true,
                    Expired = false,
                    Scores =
                    [
                        new Score { CreateDate = DateTime.Now }
                    ]
                }
            ]
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(
                [
                new ResultDetail
                {
                    ResourceId = "CalResource1",
                },
            ]));

        // Act
        await handler.HandleCondition(match);

        // Assert
        await resultDetailService.Received(1).UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        await repository.Received(1).ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }
}
