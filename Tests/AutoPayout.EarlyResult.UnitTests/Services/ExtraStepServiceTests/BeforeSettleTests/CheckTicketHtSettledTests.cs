using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;
using AutoPayout.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.BeforeSettleTests;

public class CheckTicketHtSettledTests
{
    private readonly IDynamicRepository repository = Substitute.For<IDynamicRepository>();
    private readonly IOptions<AppSettingsKey> options = Substitute.For<IOptions<AppSettingsKey>>();
    private readonly AppSettingsKey appSettingsKey;
    private readonly CheckTicketHtSettled service;

    public CheckTicketHtSettledTests()
    {
        appSettingsKey = new AppSettingsKey { SiteDbServer = new List<string> { "Server1", "Server2" } };
        options.Value.Returns(appSettingsKey);
        service = new CheckTicketHtSettled(repository, options);
    }

    [Fact]
    public async Task Process_BetRunningCountZero_SetsIsHtTicketSettledTrue()
    {
        // Arrange
        var match = new Match { MatchId = 1 };
        var settleCriteria = new SettleCriteria();

        var ticketCounts = new List<TicketCount>
        {
            new TicketCount { GroupId = 1, BetRunningCount = 0 }
        };

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Any<TicketInfoSelCriteria>())
            .Returns((null, ticketCounts));

        // Act
        await service.Process(match, settleCriteria);

        // Assert
        Assert.True(match.IsHtTicketSettled);
    }

    [Fact]
    public async Task Process_BetRunningCountNotZero_SetsIsHtTicketSettledFalse()
    {
        // Arrange
        var match = new Match { MatchId = 2 };
        var settleCriteria = new SettleCriteria();

        var ticketCounts = new List<TicketCount>
        {
            new TicketCount { GroupId = 1, BetRunningCount = 5 }
        };

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Any<TicketInfoSelCriteria>())
            .Returns((null, ticketCounts));

        // Act
        await service.Process(match, settleCriteria);

        // Assert
        Assert.False(match.IsHtTicketSettled);
    }

    [Fact]
    public async Task Process_MultipleServers_AggregatesBetRunningCount()
    {
        // Arrange
        var match = new Match { MatchId = 3 };
        var settleCriteria = new SettleCriteria();

        var ticketCountsServer1 = new List<TicketCount>
        {
            new TicketCount { GroupId = 1, BetRunningCount = 2 }
        };
        var ticketCountsServer2 = new List<TicketCount>
        {
            new TicketCount { GroupId = 1, BetRunningCount = 3 }
        };

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Is<TicketInfoSelCriteria>(c => c.DbServer == "Server1"))
            .Returns((null, ticketCountsServer1));
        repository.FetchMultipleAsync<object, TicketCount>(Arg.Is<TicketInfoSelCriteria>(c => c.DbServer == "Server2"))
            .Returns((null, ticketCountsServer2));

        // Act
        await service.Process(match, settleCriteria);

        // Assert
        // Total BetRunningCount = 5
        Assert.False(match.IsHtTicketSettled);
    }

    [Fact]
    public async Task Process_NoTicketCounts_ThrowsException()
    {
        // Arrange
        var match = new Match { MatchId = 4 };
        var settleCriteria = new SettleCriteria();

        var ticketCounts = new List<TicketCount>();

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Any<TicketInfoSelCriteria>())
            .Returns((null, ticketCounts));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.Process(match, settleCriteria));
    }
}
