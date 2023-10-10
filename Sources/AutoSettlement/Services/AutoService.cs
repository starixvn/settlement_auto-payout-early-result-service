using Fanex.Repository.Models;

namespace AutoSettlement.Services;

public interface IAutoService
{
    Task Handle();
}

public class AutoService : IAutoService
{
    private readonly ISettlementService settlementService;
    // private readonly IMatchPoolService matchPoolService;

    public AutoService(ISettlementService settlementService)
    {
        this.settlementService = settlementService;
    }

    public async Task Handle()
    {
        //   var matches = await matchPoolService.PeekNewMatch();
        var matches = new List<Match>
        {
            new()
            {
                MatchId = 123,
                Conditions = "1,2,3"
            }
        };

        matches.ForEach(m =>
         {
             Task.Run(() => settlementService.Process(m))
                 .ConfigureAwait(false);
         });
    }
}