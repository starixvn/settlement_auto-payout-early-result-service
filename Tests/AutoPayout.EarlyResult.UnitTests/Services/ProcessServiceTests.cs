using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Constants;
using AutoPayout.EarlyResult.Services;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using AutoPayout.Services;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class ProcessServiceTests
{
    private readonly WriteLogService writeLogService;

    private readonly CreateConditionChainService createConditionChainService;

    private readonly SettlementService settlementService;

    private readonly MatchPoolService matchPoolService;

    private readonly UpdateResultStatusService updateResultStatusService;

    private readonly ICondition condition;

    private readonly ProcessServiceImp service;

    public ProcessServiceTests()
    {
        writeLogService = Substitute.For<WriteLogService>();
        createConditionChainService = Substitute.For<CreateConditionChainService>();
        settlementService = Substitute.For<SettlementService>();
        matchPoolService = Substitute.For<MatchPoolService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        condition = Substitute.For<ICondition>();
        var settings = new AppSettingsKey()
        {
            RetryCount = 2,
            RetryInterval = 100
        };

        var appSettingsOptions = Options.Create(settings);
        service = new ProcessServiceImp(appSettingsOptions, writeLogService, createConditionChainService,
            settlementService, matchPoolService, updateResultStatusService);
    }

    [Fact]
    public async Task Process_Always_InvokeRelatedFunction()
    {
        var match = new Match()
        {
            Conditions = "1,2,3",
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "00",
                    ResourceStatus = 1
                }
            ],
            MatchDetail = new MatchDetail
            {
                MatchId = 1,
                ChildLeagueType = 1,
                SportType = 1,
            }
        };

        createConditionChainService.CreateConditionChain(Arg.Any<string[]>()).Returns([condition]);

        await service.Process(match);

        createConditionChainService.Received().CreateConditionChain(Arg.Is<string[]>(c => c.Length == 3));
        await settlementService.Received().UpdateResultExpired(match);
    }

    [Fact]
    public async Task Process_ExistServiceException_WriteLog()
    {
        var match = new Match()
        {
            Conditions = "1",
            ResultDetails =
            [
                new ResultDetail
                {
                    ResourceId = "00",
                    ResourceStatus = 1
                }
            ],
            MatchDetail = new MatchDetail
            {
                MatchId = 1,
                ChildLeagueType = 1,
                SportType = 1,
            }
        };

        createConditionChainService.CreateConditionChain(Arg.Any<string[]>()).Returns([condition]);
        condition.HandleCondition(Arg.Any<Match>()).ThrowsAsyncForAnyArgs(new ServiceException() { Description = "Message" });

        await service.Process(match);

        createConditionChainService.Received().CreateConditionChain(Arg.Is<string[]>(c => c.Length == 1));
        await settlementService.Received().UpdateResultExpired(match);
        await writeLogService.Received().WriteLog(match, Arg.Is<ServiceException>(e => e.Description == "Message"));
        await updateResultStatusService.Received().UpdateResultStatus(match, MatchStatus.Failed, "Message");
        await condition.Received(1).HandleCondition(Arg.Any<Match>());
        matchPoolService.Received().RemoveMatch(match);
    }

    [Fact]
    public async Task Process_UnHandledException_WriteSystemLog()
    {
        var match = new Match()
        {
            Conditions = "1"
        };

        createConditionChainService.CreateConditionChain(Arg.Any<string[]>()).Returns([condition]);
        condition.HandleCondition(Arg.Any<Match>()).ThrowsAsyncForAnyArgs(new Exception());

        await service.Process(match);

        createConditionChainService.Received().CreateConditionChain(Arg.Is<string[]>(c => c.Length == 1));
        await writeLogService.DidNotReceive().WriteLog(match, Arg.Any<ServiceException>());
        await updateResultStatusService.DidNotReceive().UpdateResultStatus(match, MatchStatus.Failed);
        await writeLogService.Received().ExceptionLog(match, Arg.Any<Exception>());
        matchPoolService.Received().RemoveMatch(match);
    }
}