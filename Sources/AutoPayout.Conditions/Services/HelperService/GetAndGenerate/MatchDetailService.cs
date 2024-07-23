namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
public interface MatchDetailService
{
    public Task<MatchDetail> GetMatchDetail(int matchId);

    public Task<Settings> GetAutoSettings(int leagueId, int sportType);
}

public class MatchDetailServiceImp(IDynamicRepository repository) : MatchDetailService
{
    public async Task<Settings> GetAutoSettings(int leagueId, int sportType)
    {
        var criteria = new GetSettingsCriteria
        {
            LeagueId = leagueId,
            SportType = sportType
        };
        return await repository.GetAsync<Settings>(criteria);
    }

    public async Task<MatchDetail> GetMatchDetail(int matchId)
    {
        return await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = matchId });
    }
}
