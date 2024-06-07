using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers.GetAndGenerate;
public class MatchDetailServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly MatchDetailService service;

    public MatchDetailServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        service = new MatchDetailServiceImp(repository);
    }

    [Fact]
    public async Task GetAutoSettings_Always_ReturnSettings()
    {
        await service.GetAutoSettings(1, 1);

        await repository.Received().GetAsync<Settings>(Arg.Is<GetSettingsCriteria>(c => c.LeagueId == 1 && c.SportType == 1));
    }

    [Fact]
    public async Task GetMatchDetail_Always_ReturnSettings()
    {
        await service.GetMatchDetail(1001);

        await repository.Received().GetAsync<Settings>(Arg.Is<GetMatchDetailCriteria>(c => c.MatchId == 1001));
    }
}
