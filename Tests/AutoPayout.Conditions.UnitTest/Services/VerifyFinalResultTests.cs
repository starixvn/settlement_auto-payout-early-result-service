using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTest.Services;
public class VerifyFinalResultTests
{
    private readonly IDynamicRepository repository;

    private readonly ResultDetailService resultDetailService;

    private readonly VerifyFinalResult condition;

    public VerifyFinalResultTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        condition = new VerifyFinalResult(repository, resultDetailService);

        var indexs = new List<ResourceIndex>()
        {
            new ()
            {
                ResourceId = "01",
                IntervalNo = 1
            },
            new ()
            {
                ResourceId = "02",
                IntervalNo = 2
            },
            new ()
            {
                ResourceId = "03",
                IntervalNo = 3
            },  new ()
            {
                ResourceId = "04",
                IntervalNo =4
            },
            new ()
            {
                ResourceId = "05",
                IntervalNo = 5
            },
        };

        resultDetailService.GetResourceIndex(Arg.Any<int>()).Returns(indexs);
    }

    [Fact]
    public async Task HandleCondition_NotMainResult_StopVerifyAsync()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.EarlyResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceId.Final
                }
            }
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        repository.DidNotReceive().Fetch<ResultDetail>(Arg.Any<GetResultDetailCriteria>());
    }

    [Fact]
    public async Task HandleCondition_NotExistFinalResult_StopVerifyAsync()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>()
        };

        await condition.HandleCondition(match);

        repository.DidNotReceive().Fetch<ResultDetail>(Arg.Any<GetResultDetailCriteria>());
    }

    [Fact]
    public async Task HandleCondition_ResultSettledAndResultInBatchNotConsistentWithFinal_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceId.Final,
                    HomeValue = "0",
                    AwayValue = "2"
                },
                new ()
                {
                    ResourceId = "01",
                    HomeValue = "2",
                    AwayValue = "1"
                },
            }
        };

        var resultsSettled = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "02",
                ResultStatusId = ResultStatusId.ManualCompleted,
                HomeValue = "1",
                AwayValue = "2"
            }

        };

        var errorMessage = "Failed: FT(0-2) is inconsistent with interval scores.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(resultsSettled);
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        await resultDetailService.Received().UpdateResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceId.Final), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Final));
    }

    [Fact]
    public async Task HandleCondition_ResultSettledAndResultInBatchConsistentWithFinal_StopVerify()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceId.Final,
                    HomeValue = "0",
                    AwayValue = "2"
                },
                new ()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "3"
                },
            }
        };

        var resultsSettled = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "02",
                ResultStatusId = ResultStatusId.ManualCompleted,
                HomeValue = "1",
                AwayValue = "2"
            }

        };

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(resultsSettled);
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        await resultDetailService.DidNotReceive().UpdateResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.DidNotReceive().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceId.Final), Arg.Any<string>());
        Assert.NotNull(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Final));
    }

    [Fact]
    public async Task HandleCondition_ResultsNotConsistent_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceId.Final,
                    HomeValue = "0",
                    AwayValue = "2"
                },
                new ()
                {
                    ResourceId = "02",
                    HomeValue = "2",
                    AwayValue = "1"
                },
            }
        };

        var errorMessage = "Failed: FT(0-2) is inconsistent with interval scores.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(new List<ResultDetail>());
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        await resultDetailService.Received().UpdateResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceId.Final), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Final));
    }

    [Fact]
    public async Task HandleCondition_FinalResultNotConsistentWithGameSession_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceId.Final,
                    HomeValue = "0",
                    AwayValue = "1"
                },
                new ()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                },
            }
        };

        var errorMessage = "Failed: FT(0-1) is inconsistent with game session information.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(new List<ResultDetail>());
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { GameSession = 3 });

        await condition.HandleCondition(match);

        await resultDetailService.Received().UpdateResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceId.Final), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Final));
    }
}
