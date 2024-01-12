using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Models;
using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class SettlementServiceTests
{
    private readonly WriteLogService writeLogService;
    private readonly IDynamicRepository repository;
    private readonly ScaleOutSettleService scaleOutSettleService;
    private readonly CreateQueryService createQueryService;
    private readonly UpdateResultStatusService updateResultStatusService;
    private readonly CreateExtraStepService createExtraStepService;
    private readonly SettlementService service;

    public SettlementServiceTests()
    {
        writeLogService = Substitute.For<WriteLogService>();
        repository = Substitute.For<IDynamicRepository>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        createQueryService = Substitute.For<CreateQueryService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        createExtraStepService = Substitute.For<CreateExtraStepService>();

        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = new List<string> { "abc", "xyz" }
        };

        var appSettingsOptions = Options.Create(settings);

        service = new SettlementServiceImp(appSettingsOptions, writeLogService, repository, scaleOutSettleService,
            createQueryService, updateResultStatusService, createExtraStepService);
    }

    [Fact]
    public async Task Push_NotExistResultDetail_NotDoAnyThing()
    {
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
        };

        await service.Push(match);

        createQueryService.DidNotReceive().QuerySettle(Arg.Any<Match>());
    }


    [Fact]
    public async Task Push_ExistResultDetail_InvokeSettleFunction()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new ()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });

        await service.Push(match);

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        createExtraStepService.Received().Create(5);
        await writeLogService.Received().WriteLogDirectly(1, Arg.Any<string>());
        await writeLogService.Received().InsertResettleLog(Arg.Any<SettleCriteria>(), 5, Arg.Any<bool>());
        await scaleOutSettleService.DidNotReceive().UpdateUnderSettlementFlag(1, Arg.Any<int>(), Arg.Any<string>());
        await repository.Received(2).ExecuteAsync(Arg.Any<SettleCriteria>());
    }


    [Fact]
    public async Task Push_ExistFinalAndHedgeMatch_InvokeAdditionalSettleFunction()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new ()
                {
                    ResourceId = "00",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            },
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });
        repository.GetAsync<HedgeMatchDetail>(Arg.Any<GetHedgeMatchCriteria>()).Returns(new HedgeMatchDetail() { MatchId = 2 });

        await service.Push(match);

        await scaleOutSettleService.Received(2).UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        createExtraStepService.Received(2).Create(5);
        await writeLogService.Received(1).WriteLogDirectly(1, Arg.Any<string>());
        await writeLogService.Received(1).WriteLogDirectly(2, Arg.Any<string>());
        await writeLogService.Received(2).InsertResettleLog(Arg.Any<SettleCriteria>(), 5, Arg.Any<bool>());
        await scaleOutSettleService.Received(2).UpdateUnderSettlementFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        await repository.Received(4).ExecuteAsync(Arg.Any<SettleCriteria>());
    }
}
