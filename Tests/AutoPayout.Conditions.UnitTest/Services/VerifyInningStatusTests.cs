using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Exceptions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyInningStatusTests
{
    private readonly IDynamicRepository repository;

    private readonly ResultDetailService resultDetailService;

    private readonly WriteLogService writeLogService;

    private readonly VerifyInningStatus condition;

    public VerifyInningStatusTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        writeLogService = Substitute.For<WriteLogService>();
        condition = new VerifyInningStatus(repository, resultDetailService, writeLogService);
    }

    [Fact]
    public async Task HandleCondition_EarlyResult_NotDoAnything()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.EarlyResource
        };

        await condition.HandleCondition(match);

        await repository.DidNotReceive().GetAsync<MatchDetail>(Arg.Any<CriteriaBase>());
    }

    [Fact]
    public async Task HandleCondition_MainResourceButNotExistFinal_NotDoAnything()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.Halftime } }
        };

        await condition.HandleCondition(match);

        await repository.DidNotReceive().GetAsync<MatchDetail>(Arg.Any<CriteriaBase>());
    }

    [Fact]
    public async Task HandleCondition_NotExistInningNeedValidate_StopValidate()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime }, new() { ResourceId = "H1" }, new() { ResourceId = "A1" } }
        };

        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 0 });

        await condition.HandleCondition(match);

        await writeLogService.DidNotReceive().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }

    [Fact]
    public async Task HandleCondition_InningNeedValidateIsCompleted_StopValidate()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime }, new() { ResourceId = "H1" } }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "A1",
                ResultStatusId = ResultStatusId.ManualCompleted
            }
        };

        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 0 });

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>())
            .Returns(resultDetails);

        await condition.HandleCondition(match);

        await writeLogService.DidNotReceive().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }

    [Fact]
    public async Task HandleCondition_InningNotExistInDB_WriteLogAndUpdateFinalResult()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = "H1" }, new() { ResourceId = ResourceIds.FullTime } }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "H3"
            }
        };

        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 0 });

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>())
            .Returns(resultDetails);

        await condition.HandleCondition(match);

        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == ResourceIds.FullTime);
        await resultDetailService.Received().UpdateResultStatus(Arg.Any<int>(),
            Arg.Is<List<ResultDetail>>(lr => lr.TrueForAll(r => r.ResourceId == ResourceIds.FullTime)), ErrorMessage.IntervalNotSettled);
        await writeLogService.Received().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }

    [Fact]
    public async Task HandleCondition_InningIsRunning_WriteLogAndUpdateFinalResult()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = "H1" }, new() { ResourceId = ResourceIds.FullTime } }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "A1",
                ResultStatusId = ResultStatusId.Running
            }
        };

        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 0 });

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>())
            .Returns(resultDetails);

        await condition.HandleCondition(match);

        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == ResourceIds.FullTime);
        await resultDetailService.Received().UpdateResultStatus(Arg.Any<int>(),
            Arg.Is<List<ResultDetail>>(lr => lr.TrueForAll(r => r.ResourceId == ResourceIds.FullTime)), ErrorMessage.IntervalNotSettled);
        await writeLogService.Received().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }
}