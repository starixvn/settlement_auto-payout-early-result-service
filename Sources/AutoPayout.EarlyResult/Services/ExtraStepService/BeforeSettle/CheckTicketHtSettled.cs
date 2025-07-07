
using Fanex.Data;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;

public class CheckTicketHtSettled : IExtraStepService
{
    private readonly IDynamicRepository repository;
    private readonly AppSettingsKey appSettingsKey;

    public CheckTicketHtSettled(IDynamicRepository repository, IOptions<AppSettingsKey> options)
    {
        this.repository = repository;
        appSettingsKey = options.Value;
    }

    public async Task Process(Match match, SettleCriteria settleCriteria)
    {
        var ticketCount = await GetTicketCounts(match.MatchId);

        var isHtTicketSettled = ticketCount is { BetRunningCount: 0 };

        match.IsHtTicketSettled = isHtTicketSettled;
    }


    private async Task<TicketCount> GetTicketCounts(int matchId)
    {
        const int halfTimeGroupId = 1;

        var criteria = new TicketInfoSelCriteria
        {
            MatchId = matchId
        };
        var tasks = appSettingsKey.SiteDbServer
            .Select(s => GetTicketCountsBySite(criteria, s))
            .ToList();

        var result = await Task.WhenAll(tasks);

        var x = result.SelectMany(r => r)
            .Where(r => r.GroupId == halfTimeGroupId)
            .GroupBy(r => r.GroupId)
            .Select(g => new TicketCount
            {
                BetRunningCount = g.Sum(v => v.BetRunningCount),
                BetMarkedCount = g.Sum(v => v.BetMarkedCount),
                BetCount = g.Sum(v => v.BetCount),
                BetSettledCount = g.Sum(v => v.BetSettledCount),
                BetWaitingCount = g.Sum(v => v.BetWaitingCount)
            });

        return x.First();
    }

    private async Task<List<TicketCount>> GetTicketCountsBySite(TicketInfoSelCriteria criteria, string server)
    {
        criteria.DbServer = server;

        var (_, ticketCounts) = await repository.FetchMultipleAsync<object, TicketCount>(criteria);

        return ticketCounts.ToList();
    }
}


public class TicketInfoSelCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey()
    {
        return $"NAP_Set_MatchSelDetail_Ticket_{DbServer}";
    }

    public override bool IsValid() => MatchId > 0;
}

public class TicketCount
{
    public int GroupId { get; set; }

    public int BetCount { get; set; }

    public int BetRunningCount { get; set; }

    public int BetSettledCount { get; set; }

    public int BetMarkedCount { get; set; }
    public int BetWaitingCount { get; set; }
}