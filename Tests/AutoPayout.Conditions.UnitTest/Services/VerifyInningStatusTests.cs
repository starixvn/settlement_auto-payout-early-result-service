using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Exceptions;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

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
        var appSettingOptions = Substitute.For<IOptions<AppSettingsKey>>();
        condition = new VerifyInningStatus(repository, resultDetailService, writeLogService, appSettingOptions);
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
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime }, new() { ResourceId = "H1" }, new() { ResourceId = "A1" } },
            MatchDetail = new MatchDetail() { GameSession = 0 }
        };

        await condition.HandleCondition(match);

        await writeLogService.DidNotReceive().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }

    [Fact]
    public async Task HandleCondition_InningNeedValidateIsCompleted_StopValidate()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime }, new() { ResourceId = "H1" } },
            MatchDetail = new MatchDetail() { GameSession = 0 }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "A1",
                ResultStatusId = ResultStatusId.ManualCompleted
            }
        };

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
            ResultDetails = new List<ResultDetail> { new() { ResourceId = "H1" }, new() { ResourceId = ResourceIds.FullTime } },
            MatchDetail = new MatchDetail() { GameSession = 0 }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "H3"
            }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>())
            .Returns(resultDetails);

        await condition.HandleCondition(match);

        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == ResourceIds.FullTime);
        await resultDetailService.Received().UpdateInvalidResultStatus(Arg.Any<int>(),
            Arg.Is<List<ResultDetail>>(lr => lr.TrueForAll(r => r.ResourceId == ResourceIds.FullTime)), ErrorMessage.IntervalNotSettled);
        await writeLogService.Received().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }

    [Fact]
    public async Task HandleCondition_InningIsRunning_WriteLogAndUpdateFinalResult()
    {
        var match = new Match()
        {
            ResourceType = ResourceType.MainResource,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = "H1" }, new() { ResourceId = ResourceIds.FullTime } },
            MatchDetail = new MatchDetail() { GameSession = 0 }
        };

        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "A1",
                ResultStatusId = ResultStatusId.Running
            }
        };

        repository.FetchAsync<ResultDetail>(Arg.Any<GetResultDetailCriteria>())
            .Returns(resultDetails);

        await condition.HandleCondition(match);

        Assert.DoesNotContain(match.ResultDetails, r => r.ResourceId == ResourceIds.FullTime);
        await resultDetailService.Received().UpdateInvalidResultStatus(Arg.Any<int>(),
            Arg.Is<List<ResultDetail>>(lr => lr.TrueForAll(r => r.ResourceId == ResourceIds.FullTime)), ErrorMessage.IntervalNotSettled);
        await writeLogService.Received().WriteLog(Arg.Any<Match>(), Arg.Any<IntervalNotSettledException>());
    }
}