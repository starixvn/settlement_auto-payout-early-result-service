using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;
public class MatchServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly MatchService matchService;
    private readonly ICache cache;

    public MatchServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        cache = new RedisCache(new RedisCacheOptions
        {
            ConnectionString = "10.18.200.131:16379,password=foo1@bared,defaultDatabase=11",
            ApplicationName = "Settlement-LOCAL"
        });

        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = new List<string> { "abc" },
            RetryCount = 1,
            CacheKey = "Settlement-AutoPayoutOtherSport-LeagueId"
        };

        var appSettingsOptions = Options.Create(settings);

        matchService = new MatchServiceImp(appSettingsOptions, cache, repository);
    }

    [Fact]
    public async Task GetMatches_ExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
        // Arrange
        var results = new List<ResultDetail>
            {
                new () { MatchId = 1, LeagueId = 123, SportType = 5},
                new () { MatchId = 2, LeagueId = 111123, SportType = 5 }
            };
        var conditions = new List<ConditionSetting>()
        {
            new ()
            {
                SportType = 5,
                EarlyCheckList = "1,2,3",
                MainCheckList = "4,5,6"
            },
        };
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync("Settlement-AutoPayoutOtherSport-LeagueId", "111123,456", cachePolicy);

        // Act
        var list = await matchService.GetMatches();

        // Assert
        Assert.Single(list);
    }


    [Fact]
    public async Task GetMatches_NotExistConditions_NotReturnThatMatch()
    {
        // Arrange
        var results = new List<ResultDetail>
        {
            new () { MatchId = 1, LeagueId = 123, SportType = 5, ResourceType = ResourceType.MainResource},
            new () { MatchId = 2, LeagueId = 111123, SportType = 6 }
        };
        var conditions = new List<ConditionSetting>()
        {
            new ()
            {
                SportType = 5,
                EarlyCheckList = "1,2,3",
                MainCheckList = "4,5,6"
            }
        };
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync("Settlement-AutoPayoutOtherSport-LeagueId", "", cachePolicy);

        // Act
        var list = await matchService.GetMatches();

        // Assert
        Assert.Single(list);
        Assert.Equal(1, list[0].MatchId);
        Assert.Equal("4,5,6", list[0].Conditions);
    }
}
