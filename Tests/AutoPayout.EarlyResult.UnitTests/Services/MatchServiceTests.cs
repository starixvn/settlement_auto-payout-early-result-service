using AutoPayout.Conditions.Constants;
using AutoPayout.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services;
using AutoPayout.Models;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Settlement.Converter.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class MatchServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly MatchServiceImp matchService;
    private readonly ICache cache;
    private readonly XmlConvertService xmlConvertService;

    public MatchServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        cache = new RedisCache(new RedisCacheOptions
        {
            ConnectionString = "10.18.200.131:16379,password=foo1@bared,defaultDatabase=11",
            ApplicationName = "Settlement-LOCAL"
        });

        var settings = new AppSettingsKey()
        {
            EnvironmentName = "PRO",
            SiteDbServer = ["abc"],
            RetryCount = 1,
            CacheKey = CacheKey.LeagueId
        };

        var appSettingsOptions = Options.Create(settings);

        matchService = new MatchServiceImp(appSettingsOptions, cache, repository, xmlConvertService);
    }

    [Fact]
    public async Task GetMatches_ExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
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
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results, []));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync(CacheKey.LeagueId, "111123,456", cachePolicy);

        var list = await matchService.GetMatches();

        Assert.Single(list);
    }

    [Fact]
    public async Task GetMatches_NotExistConditions_NotReturnThatMatch()
    {
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
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results, []));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync(CacheKey.LeagueId, "", cachePolicy);

        var list = await matchService.GetMatches();

        Assert.Single(list);
        Assert.Equal(1, list[0].MatchId);
        Assert.Equal("4,5,6", list[0].Conditions);
    }

    [Fact]
    public async Task GetMatches_IgnoreResultExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
        var results = new List<ResultDetail>
        {
            new ()
            {
                MatchId = 1,
                ResourceId = ResourceIds.OverTime,
                LeagueId = 123,
                SportType = SportType.BasketBall,
            },
            new ()
            {
                MatchId = 2,
                ResourceId = ResourceIds.FullTime,
                LeagueId = 123,
                SportType = SportType.BasketBall
            }
        };
        var conditions = new List<ConditionSetting>()
        {
            new ()
            {
                SportType = SportType.BasketBall,
                EarlyCheckList = "1,2,3",
                MainCheckList = "4,5,6"
            },
        };
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results, []));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync(CacheKey.LeagueId, "456", cachePolicy);

        var list = await matchService.GetMatches();
        const int expectedMatchId = 2;
        Assert.Equal(expectedMatchId, list[0].MatchId);
    }

    [Fact]
    public async Task GetMatches_ResourceStatusExpiredExistedIncludeLeagueIdAndExcludeLeagueId_ReturnsRightMatches()
    {
        var results = new List<ResultDetail>
        {
            new ()
            {
                MatchId = 1,
                ResourceId = ResourceIds.OverTime,
                LeagueId = 123,
                SportType = SportType.BasketBall,
                ResourceStatus = ResourceStatus.Expired
            },
            new ()
            {
                MatchId = 2,
                ResourceId = ResourceIds.FullTime,
                LeagueId = 123,
                SportType = SportType.BasketBall
            }
        };
        var conditions = new List<ConditionSetting>()
        {
            new ()
            {
                SportType = SportType.BasketBall,
                EarlyCheckList = "1,2,3",
                MainCheckList = "4,5,6"
            },
        };
        repository.FetchMultipleAsync<ConditionSetting, ResultDetail, ResultDetail>(Arg.Any<GetResultCriteria>()).Returns((conditions, results, []));
        var cachePolicy = new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
        await cache.SetAsync(CacheKey.LeagueId, "456", cachePolicy);

        var list = await matchService.GetMatches();
        const int expectedMatchId = 1;
        Assert.Equal(expectedMatchId, list[0].MatchId);
        Assert.Equal(2, list.Count);
    }
}