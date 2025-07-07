using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;
using AutoPayout.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.BeforeSettleTests;

public class CheckTicketHtSettledTests
{
    private readonly IDynamicRepository repository = Substitute.For<IDynamicRepository>();
    private readonly IOptions<AppSettingsKey> options;
    private readonly AppSettingsKey appSettingsKey;
    private readonly CheckTicketHtSettled service;

    public CheckTicketHtSettledTests()
    {
        appSettingsKey = new AppSettingsKey
        {
            SiteDbServer = ["Server1"]
        };
        options = Substitute.For<IOptions<AppSettingsKey>>();
        options.Value.Returns(appSettingsKey);

        service = new CheckTicketHtSettled(repository, options);
    }

    [Fact]
    public async Task Process_Should_Set_IsHtTicketSettled_True_When_BetRunningCount_Is_Zero()
    {
        // Arrange
        var match = new Match { MatchId = 1 };
        var settleCriteria = new SettleCriteria();

        var ticketCounts = new List<TicketCount>
        {
            new() {
                GroupId = 1,
                BetRunningCount = 0,
                BetMarkedCount = 2,
                BetCount = 2,
                BetSettledCount = 2,
                BetWaitingCount = 0
            }
        };

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Any<TicketInfoSelCriteria>())
            .Returns((null, ticketCounts));

        // Act
        await service.Process(match, settleCriteria);

        // Assert
        Assert.True(match.IsHtTicketSettled);
    }

    [Fact]
    public async Task Process_Should_Set_IsHtTicketSettled_False_When_BetRunningCount_Is_Not_Zero()
    {
        // Arrange
        var match = new Match { MatchId = 1 };
        var settleCriteria = new SettleCriteria();

        var ticketCounts = new List<TicketCount>
        {
            new TicketCount
            {
                GroupId = 1,
                BetRunningCount = 5,
                BetMarkedCount = 2,
                BetCount = 7,
                BetSettledCount = 2,
                BetWaitingCount = 0
            }
        };

        repository.FetchMultipleAsync<object, TicketCount>(Arg.Any<TicketInfoSelCriteria>())
            .Returns((null, ticketCounts));

        // Act
        await service.Process(match, settleCriteria);

        // Assert
        Assert.False(match.IsHtTicketSettled);
    }
}
