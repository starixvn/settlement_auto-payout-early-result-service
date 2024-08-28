namespace AutoPayout.EarlyResult.Services;

public interface IAutoService
{
    Task Handle();
}

public class AutoService(ProcessService processService, MatchPoolService matchPoolService)
    : IAutoService
{
    public async Task Handle()
    {
        var matches = await matchPoolService.GetNewMatches();
        processService.Process(matches[0]);
        //matches.ForEach(m =>
        // {
        //     Task.Run(() => processService.Process(m))
        //         .ConfigureAwait(false);
        // });
    }
}