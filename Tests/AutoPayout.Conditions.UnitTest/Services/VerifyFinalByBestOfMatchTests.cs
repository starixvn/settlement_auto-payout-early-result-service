using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalByBestOfMatchTests
{
    private readonly IDynamicRepository repository;

    private readonly ResultDetailService resultDetailService;

    private readonly VerifyFinalByBestOfMap condition;

    public VerifyFinalByBestOfMatchTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        condition = new VerifyFinalByBestOfMap(resultDetailService);
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
                    ResourceId = ResourceIds.FullTime
                }
            }
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>()).Returns(new MatchDetail() { BestOfMap = 3 });

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
            ResultDetails = new List<ResultDetail>()
        };

        await condition.HandleCondition(match);

        await resultDetailService.DidNotReceive().GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>());
    }

    [Fact]
    public async Task HandleConditionBO3_ResultSettledAndResultInBatchConsistentWithFinal_Passed()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>()
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 3 }
        };

        var resultsCalculated = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "3",
                IntervalNo = 1,
                SettlementStatusId = SettlementStatusId.Calculated
            },
        };

        var results = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "3",
                IntervalNo = 1,
                SettlementStatusId = SettlementStatusId.Calculated
            },
            new()
            {
                ResourceId = "M2",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2,
            },
        };

        resultDetailService.GetResultSettled(1, Arg.Any<int>()).Returns(resultsCalculated);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>()).Returns((results, new List<ResourceIndex>()));

        await condition.HandleCondition(match);

        await resultDetailService.DidNotReceive().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>());
        resultDetailService.DidNotReceive().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), Arg.Any<string>());
        Assert.NotNull(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleConditionBO3_ResultSettledAndResultInBatchNotConsistentWithFinal_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 3 }
        };

        var resultsCalculated = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "3",
                IntervalNo = 1,
                SettlementStatusId = SettlementStatusId.Calculated
            },
        };

        var results = new List<ResultDetail>()
        {
            new ()
            {
                ResourceId = "M1",
                HomeValue = "2",
                AwayValue = "1",
                IntervalNo = 1
            },
            new()
            {
                ResourceId = "M2",
                ResultStatusId = ResultStatusId.ManualCompleted,
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2
            },
        };

        var errorMessage = "Failed Auto Payout: FT(0-2) is inconsistent with Map information.";

        resultDetailService.GetResultSettled(1, Arg.Any<int>()).Returns(resultsCalculated);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>()).Returns((results, new List<ResourceIndex>()));

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleConditionBO3_ResultsNotConsistent_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 3 }
        };

        var resultsCalculated = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "3",
                IntervalNo = 1,
                SettlementStatusId = SettlementStatusId.Calculated
            },
        };

        var results = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M2",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 2
            },
        };

        var errorMessage = "Failed Auto Payout: FT(0-2) is inconsistent with Map information.";

        resultDetailService.GetResultSettled(1, Arg.Any<int>()).Returns(resultsCalculated);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>()).Returns((results, new List<ResourceIndex>()));

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleConditionBO3_FinalResultNotConsistentWithGameSession_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "1"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 3 }
        };

        var errorMessage = "Failed Auto Payout: FT(0-1) is inconsistent with Map information.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(new List<ResultDetail>());

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleConditionBO1_FinalResultNotConsistent_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "1",
                    AwayValue = "1"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 1 }
        };

        var results = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "1",
                IntervalNo = 1
            },
        };

        var errorMessage = "Failed Auto Payout: FT(1-1) is inconsistent with Map information.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(results);

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleConditionBO2_FinalResultNotConsistent_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 2 }
        };

        var results = new List<ResultDetail>()
        {
            new()
            {
                ResourceId = "M1",
                HomeValue = "1",
                AwayValue = "2",
                IntervalNo = 1
            },
            new()
            {
                ResourceId = "M2",
                HomeValue = "2",
                AwayValue = "1",
                IntervalNo = 2
            },
        };

        var errorMessage = "Failed Auto Payout: FT(0-2) is inconsistent with Map information.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(results);

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }

    [Fact]
    public async Task HandleCondition_NotExistResults_UpdateStatusFinalToInvalid()
    {
        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "0",
                    AwayValue = "2"
                }
            },
            MatchDetail = new MatchDetail() { BestOfMap = 2 }
        };

        var errorMessage = "Failed Auto Payout: FT(0-2) is inconsistent with Map information.";

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>()).Returns(new List<ResultDetail>());

        await Assert.ThrowsAsync<ServiceException>(async () => await condition.HandleCondition(match));

        await resultDetailService.Received().UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), errorMessage);
        resultDetailService.Received().WriteLogResult(1, Arg.Is<ResultDetail>(r => r.ResourceId == ResourceIds.FullTime), errorMessage);
        Assert.Null(match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime));
    }
}