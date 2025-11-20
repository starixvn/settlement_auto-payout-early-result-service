using AutoPayout.Conditions.Constants;
using AutoPayout.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Models;
using AutoPayout.EarlyResult.Services;
using AutoPayout.EarlyResult.Services.ExtraStepService;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Settlement.MessageBridge.JackTime;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using MassTransit;
using MassTransit.Transports;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class SettlementServiceTests
{
    private readonly WriteLogService writeLogService;
    private readonly IDynamicRepository repository;
    private readonly ScaleOutSettleService scaleOutSettleService;
    private readonly CreateQueryService createQueryService;
    private readonly CreateExtraStepService createExtraStepService;
    private readonly UpdateResultStatusService updateResultStatusService;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly SettlementService service;

    public SettlementServiceTests()
    {
        writeLogService = Substitute.For<WriteLogService>();
        repository = Substitute.For<IDynamicRepository>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        createQueryService = Substitute.For<CreateQueryService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        createExtraStepService = Substitute.For<CreateExtraStepService>();
        publishEndpoint = Substitute.For<IPublishEndpoint>();

        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = ["abc", "xyz"]
        };

        var appSettingsOptions = Options.Create(settings);

        var dependencies = new SettlementDependencies(scaleOutSettleService, createQueryService, updateResultStatusService, createExtraStepService);
        service = new SettlementServiceImp(appSettingsOptions, writeLogService, repository, dependencies, publishEndpoint);
    }

    [Fact]
    public async Task Push_NotExistResultDetail_NotDoAnyThing()
    {
        // Arrange
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>()
        };

        // Act
        await service.Push(match);

        // Assert
        createQueryService.DidNotReceive().QuerySettle(Arg.Any<Match>());
    }

    [Fact]
    public async Task Push_ExistResultDetail_InvokeSettleFunction()
    {
        // Arrange
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

        // Act
        await service.Push(match);

        // Assert
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        createExtraStepService.Received().Create(match);
        await writeLogService.Received().WriteLogDirectly(1, Arg.Any<string>());
        await writeLogService.Received().InsertResettleLog(Arg.Any<SettleCriteria>(), 5, Arg.Any<bool>());
        await scaleOutSettleService.DidNotReceive().UpdateUnderSettlementFlag(1, Arg.Any<int>(), Arg.Any<string>());
        await repository.Received(2).ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task Push_ExistFinalAndHedgeMatch_InvokeAdditionalSettleFunction()
    {
        // Arrange
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

        // Act
        await service.Push(match);

        // Assert
        await scaleOutSettleService.Received(2).UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        createExtraStepService.Received(1).Create(match);
        await writeLogService.Received(1).WriteLogDirectly(1, Arg.Any<string>());
        await writeLogService.Received(1).WriteLogDirectly(2, Arg.Any<string>());
        await writeLogService.Received(2).InsertResettleLog(Arg.Any<SettleCriteria>(), 5, Arg.Any<bool>());
        await scaleOutSettleService.Received(2).UpdateUnderSettlementFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        await repository.Received(4).ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task UpdateResultExpired_NoExpiredResult_DoesNothing()
    {
        // Arrange
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = "01", ResourceStatus = 1 },
                new() { ResourceId = "02", ResourceStatus = 1 }
            }
        };

        // Act
        await service.UpdateResultExpired(match);

        // Assert
        await writeLogService.DidNotReceive().WriteLog(Arg.Any<Match>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateResultExpired_HasExpiredResult_UpdatesAndLogs()
    {
        // Arrange
        var expired = new ResultDetail { ResourceId = "01", ResourceStatus = 2 };
        var valid = new ResultDetail { ResourceId = "02", ResourceStatus = 1 };
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { expired, valid }
        };

        // Act
        await service.UpdateResultExpired(match);

        // Assert
        Assert.Single(match.ResultDetails);
        Assert.Equal("02", match.ResultDetails[0].ResourceId);
        await writeLogService.Received(1).WriteLog(Arg.Is<Match>(m => m.ResultDetails.Count == 1 && m.ResultDetails[0].ResourceId == "01"), Arg.Any<string>());
    }

    [Fact]
    public async Task Push_WithBasketBallSportType_PublishesHybridParlay()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = SportType.BasketBall,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        var settleCriteria = new SettleCriteria { MatchId = 1, Action = "TestAction", AdminId = 123, SelectedResources = "01" };
        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(settleCriteria);

        await service.Push(match);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<HybridMatch>(h => h.MatchId == 1 && h.Action == "TestAction" && h.UserId == 123),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Push_WithFinalResource_UpdatesUnderSettlementFlag()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        var settleCriteria = new SettleCriteria { MatchId = 1, AdminId = 123, Action = "TestAction" };
        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(settleCriteria);

        await service.Push(match);

        await scaleOutSettleService.Received(1).UpdateUnderSettlementFlag(1, 123, "TestAction");
    }

    [Fact]
    public async Task Push_WithProcessAfterSettle_InvokesAllExtraSteps()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        var extraStep1 = Substitute.For<IExtraStepService>();
        var extraStep2 = Substitute.For<IExtraStepService>();
        var extraSteps = new List<IExtraStepService> { extraStep1, extraStep2 };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });
        createExtraStepService.CreateStepsServiceAfterSettle(match).Returns(extraSteps);

        await service.Push(match);

        await extraStep1.Received(1).Process(Arg.Any<Match>(), Arg.Any<SettleCriteria>());
        await extraStep2.Received(1).Process(Arg.Any<Match>(), Arg.Any<SettleCriteria>());
        await updateResultStatusService.Received(2).UpdateResultStatus(Arg.Any<Match>(), MatchStatus.Complete);
    }

    [Fact]
    public async Task Push_WithKabaddiSportType_UpdatesMatchInfoWithScores()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = SportType.Kabaddi,
            IsHtTicketSettled = true,
            MatchDetail = new MatchDetail
            {
                HalfTimeHomeScore = 10,
                HalfTimeAwayScore = 15
            },
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = ResourceIds.FullTime,
                    HomeValue = "20",
                    AwayValue = "25"
                }
            }
        };

        var settleCriteria = new SettleCriteria { MatchId = 1, Action = "TestAction", AdminId = 123, BetStatus = "Test", SelectedResources = "00" };
        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(settleCriteria);

        await service.Push(match);

        await scaleOutSettleService.Received(1).UpdateMatchInfo(
            Arg.Is<UpdateMatchInfoRequest>(r =>
                r.MatchId == 1 &&
                r.HalfTimeHome == 10 &&
                r.HalfTimeAway == 15 &&
                r.FullTimeHome == 20 &&
                r.FullTimeAway == 25 &&
                r.IsHtTicketSettled == true));
    }

    [Fact]
    public async Task Push_WithEmptySiteDbServer_DoesNotExecuteSettle()
    {
        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = []
        };

        var appSettingsOptions = Options.Create(settings);
        var dependencies = new SettlementDependencies(scaleOutSettleService, createQueryService, updateResultStatusService, createExtraStepService);
        var serviceWithEmptyServers = new SettlementServiceImp(appSettingsOptions, writeLogService, repository, dependencies, publishEndpoint);

        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });

        await serviceWithEmptyServers.Push(match);

        await repository.DidNotReceive().ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task Push_WithMultipleSiteDbServers_ExecutesSettleForEachServer()
    {
        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = ["server1", "server2", "server3"]
        };

        var appSettingsOptions = Options.Create(settings);
        var dependencies = new SettlementDependencies(scaleOutSettleService, createQueryService, updateResultStatusService, createExtraStepService);
        var serviceWithMultipleServers = new SettlementServiceImp(appSettingsOptions, writeLogService, repository, dependencies, publishEndpoint);

        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });

        await serviceWithMultipleServers.Push(match);

        await repository.Received(3).ExecuteAsync(Arg.Any<SettleCriteria>());
    }

    [Fact]
    public async Task UpdateResultExpired_WithMultipleExpiredResults_LogsAllExpiredIds()
    {
        var expired1 = new ResultDetail { ResourceId = "01", ResourceStatus = ResourceStatus.Expired };
        var expired2 = new ResultDetail { ResourceId = "02", ResourceStatus = ResourceStatus.Expired };
        var expired3 = new ResultDetail { ResourceId = "03", ResourceStatus = ResourceStatus.Expired };
        var valid = new ResultDetail { ResourceId = "04", ResourceStatus = 1 };

        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { expired1, expired2, expired3, valid }
        };

        await service.UpdateResultExpired(match);

        Assert.Single(match.ResultDetails);
        Assert.Equal("04", match.ResultDetails[0].ResourceId);
        await writeLogService.Received(1).WriteLog(
            Arg.Is<Match>(m => m.ResultDetails.Count == 3),
            Arg.Is<string>(s => s.Contains("01") && s.Contains("02") && s.Contains("03")));
        await updateResultStatusService.Received(1).UpdateResultStatus(
            Arg.Is<Match>(m => m.ResultDetails.Count == 3),
            MatchStatus.Failed,
            Arg.Any<string>());
    }

    [Fact]
    public async Task Push_UpdatesResultStatusToWaitingAndComplete()
    {
        var match = new Match()
        {
            MatchId = 1,
            SportType = 5,
            ResultDetails = new List<ResultDetail>()
            {
                new()
                {
                    ResourceId = "01",
                    HomeValue = "1",
                    AwayValue = "2"
                }
            }
        };

        createQueryService.QuerySettle(Arg.Any<Match>()).Returns(new SettleCriteria { MatchId = 1 });

        await service.Push(match);

        await updateResultStatusService.Received(1).UpdateResultStatus(Arg.Any<Match>(), MatchStatus.Waiting);
        await updateResultStatusService.Received(2).UpdateResultStatus(Arg.Any<Match>(), MatchStatus.Complete);
    }
}