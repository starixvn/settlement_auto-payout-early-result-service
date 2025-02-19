using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalResultTests
{
    private readonly IDynamicRepository repository;

    private readonly ResultDetailService resultDetailService;

    private readonly VerifyFinalResult condition;
    private readonly IOptions<AppSettingsKey> appSettingsOptions;

    public VerifyFinalResultTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        appSettingsOptions = Substitute.For<IOptions<AppSettingsKey>>();
        condition = new VerifyFinalResult(resultDetailService, appSettingsOptions);
    }

    [Fact]
    public async Task HandleCondition_NotMainResult_StopVerifyAsync()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.EarlyResource,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = ResourceIds.FullTime
                }
            ]
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        await resultDetailService.DidNotReceive().GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>());
    }

    [Fact]
    public async Task HandleCondition_NotExistFinalResult_StopVerifyAsync()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = []
        };

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.DidNotReceive().GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>());
    }

    [Fact]
    public async Task HandleCondition_ResultSettledAndResultInBatchNotConsistentWithFinal_UpdateStatusFinalToInvalid()
    {
        // Arrange
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            ],
            MatchDetail = new MatchDetail() { GameSession = 3 }
        };

        var results = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "1",
                IntervalNo = 1
            },
            new()
            {
                ResourceId = "02",
                ResultStatusId = ResultStatusId.ManualCompleted,
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2
            }
        };

        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns(results);
        resultDetailService.GetValidSetResults(Arg.Any<Match>(), Arg.Any<List<ResultDetail>>(), Arg.Any<IEnumerable<ResourceIndex>>(), Arg.Any<int>()).Returns(results);

        appSettingsOptions.Value.Returns(new AppSettingsKey { RetryInterval = 3000 });
        resultDetailService.MaxTimeScore(Arg.Any<ResultDetail>()).Returns(2);

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), Arg.Any<string>());
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleCondition_ResultSettledAndResultInBatchConsistentWithFinal_StopVerify()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            ],
            MatchDetail = new MatchDetail() { GameSession = 3 }
        };

        var results = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "3",
                ResultStatusId = 2,
                IntervalNo = 1
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2,
                 ResultStatusId = 2,
            },
        };

        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns(results);
        resultDetailService.GetValidSetResults(Arg.Any<Match>(), Arg.Any<List<ResultDetail>>(), Arg.Any<IEnumerable<ResourceIndex>>(), Arg.Any<int>()).Returns(results);

        appSettingsOptions.Value.Returns(new AppSettingsKey { RetryInterval = 3000 });
        resultDetailService.MaxTimeScore(Arg.Any<ResultDetail>()).Returns(2);

        resultDetailService.GetResourceIndex(Arg.Any<int>()).Returns(
        [
            new ResourceIndex
            {
                ResourceId = "01",
                IntervalNo = 1
            },
            new ResourceIndex
            {
                ResourceId = "02",
                IntervalNo = 2
            },
            new ResourceIndex
            {
                ResourceId = "03",
                IntervalNo = 3
            },
            new ResourceIndex
            {
                ResourceId = "04",
                IntervalNo = 4
            },
            new ResourceIndex
            {
                ResourceId = "05",
                IntervalNo = 5
            }
        ]);

        await condition.HandleCondition(match);

        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.DidNotReceive().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), Arg.Any<string>());
        Assert.NotNull(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleCondition_ResultsNotConsistent_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            ],
            MatchDetail = new MatchDetail() { GameSession = 3 }
        };

        var results = new List<ResultDetail>()
        {
                        new()
            {
                ResourceId = "01",
                HomeValue = "4",
                AwayValue = "2",
                IntervalNo = 1
            },
            new()
            {
                ResourceId = "02",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2
            },
        };


        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns(results);
        resultDetailService.GetValidSetResults(Arg.Any<Match>(), Arg.Any<List<ResultDetail>>(), Arg.Any<IEnumerable<ResourceIndex>>(), Arg.Any<int>()).Returns(results);

        appSettingsOptions.Value.Returns(new AppSettingsKey { RetryInterval = 3000 });
        resultDetailService.MaxTimeScore(Arg.Any<ResultDetail>()).Returns(2);

        resultDetailService.GetResourceIndex(Arg.Any<int>()).Returns(
        [
            new ResourceIndex
            {
                ResourceId = "01",
                IntervalNo = 1
            },
            new ResourceIndex
            {
                ResourceId = "02",
                IntervalNo = 2
            },
            new ResourceIndex
            {
                ResourceId = "03",
                IntervalNo = 3
            },
            new ResourceIndex
            {
                ResourceId = "04",
                IntervalNo = 4
            },
            new ResourceIndex
            {
                ResourceId = "05",
                IntervalNo = 5
            }
        ]);

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received(1).UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), Arg.Any<string>());
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleCondition_FinalResultNotConsistentWithGameSession_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails =
            [
                new()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "1"
                }
            ],
            MatchDetail = new MatchDetail() { GameSession = 3 }
        };

        var results = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 1
            },
        };

        const string errorMessage = "Failed Auto Payout: FT(0-1) is inconsistent with game session information.";

        resultDetailService.GetResultSettled(1, Arg.Any<int>()).Returns(results);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>()).Returns((results, new List<ResourceIndex>()));

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleCondition_NotExistResultChecking_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            SportType = SportType.Tennis,
            ResourceType = ResourceType.MainResource,
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            ],
            MatchDetail = new MatchDetail() { GameSession = 3 }
        };

        appSettingsOptions.Value.Returns(new AppSettingsKey { RetryInterval = 3000 });
        resultDetailService.MaxTimeScore(Arg.Any<ResultDetail>()).Returns(2);
        resultDetailService.GetResourceIndex(Arg.Any<int>()).Returns(
        [
            new ResourceIndex
            {
                ResourceId = "01",
                IntervalNo = 1
            },
            new ResourceIndex
            {
                ResourceId = "02",
                IntervalNo = 2
            },
            new ResourceIndex
            {
                ResourceId = "03",
                IntervalNo = 3
            },
            new ResourceIndex
            {
                ResourceId = "04",
                IntervalNo = 4
            },
            new ResourceIndex
            {
                ResourceId = "05",
                IntervalNo = 5
            }
        ]);
        resultDetailService.GetResultSettled(1, Arg.Any<int>()).Returns([]);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>()).Returns((new List<ResultDetail>(), new List<ResourceIndex>()));

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), Arg.Any<string>());
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }
}