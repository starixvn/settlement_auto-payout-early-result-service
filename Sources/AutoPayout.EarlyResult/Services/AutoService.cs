namespace AutoPayout.EarlyResult.Services;

public interface IAutoService
{
    Task Handle();
}

public class AutoService : IAutoService
{
    private readonly ProcessService processService;
    private readonly MatchPoolService matchPoolService;

    public AutoService(ProcessService processService, MatchPoolService matchPoolService)
    {
        this.processService = processService;
        this.matchPoolService = matchPoolService;
    }

    public async Task Handle()
    {
        var matches = await matchPoolService.GetNewMatches();

        matches.ForEach(m =>
         {
             Task.Run(() => processService.Process(m))
                 .ConfigureAwait(false);
         });
    }
}