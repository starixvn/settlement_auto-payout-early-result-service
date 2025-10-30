using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyRelatePlayerResultTests
{
    private readonly IDynamicRepository _repository = Substitute.For<IDynamicRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly VerifyRelatePlayerResult _sut;

    public VerifyRelatePlayerResultTests()
    {
        _sut = new VerifyRelatePlayerResult(_repository, _cache);
    }

    [Fact]
    public async Task HandleCondition_NoPlayerResults_NoChanges()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail> { new() { ResourceId = "00" } }
        };

        await _sut.HandleCondition(match);

        Assert.Single(match.ResultDetails);
        await _repository.DidNotReceive().FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>());
    }

    [Fact]
    public async Task HandleCondition_PlayerDependent_ValidRelated_SetsValidAndRelatedFields()
    {
        // Arrange a player dependent result (prefix669)
        var playerResult = new ResultDetail { ResourceId = "66912345" };
        var match = new Match { MatchId = 11, ResultDetails = new List<ResultDetail> { playerResult } };

        var primaryInfo = new OddsInfoModel
        {
            PlayerId = 10,
            TeamId = 1,
            IsHome = true,
            Statistics = new List<Statistic>
            {
                new() { Type = "points", Value = "10+" },
                new() { Type = "rebounds", Value = "5+" }
            }
        };
        var primaryOddFlex = new OddFlexModel
        {
            ResourceId = playerResult.ResourceId,
            BetType = "any",
            RelatedBettype = "653,659,00",
            OddsInfo = JsonConvert.SerializeObject(primaryInfo)
        };
        var odd653 = new OddFlexModel
        {
            ResourceId = "653777",
            OddsInfo = JsonConvert.SerializeObject(new OddsInfoModel { PlayerId = 10, TeamId = 1 })
        };
        var odd659 = new OddFlexModel
        {
            ResourceId = "659888",
            OddsInfo = JsonConvert.SerializeObject(new OddsInfoModel { PlayerId = 10, TeamId = 1 })
        };
        _repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>())
            .Returns(Task.FromResult<IEnumerable<OddFlexModel>>(new[] { primaryOddFlex, odd653, odd659 }));

        var relatedResults = new List<ResultDetail>
        {
            new() { ResourceId = "653777", SettlementStatusId =4 },
            new() { ResourceId = "659888", SettlementStatusId =4 },
            new() { ResourceId = "00", SettlementStatusId =4 }
        };
        _repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(relatedResults));

        // Act
        await _sut.HandleCondition(match);

        // Assert
        var result = match.ResultDetails.Single();
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.RelatedCalResources);
        Assert.Contains("653777", result.RelatedCalResources);
        Assert.Contains("659888", result.RelatedCalResources);
        Assert.Contains("00", result.RelatedCalResources);
        Assert.Equal(3, result.RelatedResults.Count);
        Assert.NotEmpty(result.OddFlex);
    }

    [Fact]
    public async Task HandleCondition_PlayerDependent_MissingRelated_MarksInvalidAndFiltersOut()
    {
        var playerResult = new ResultDetail { ResourceId = "66912345" };
        var match = new Match { MatchId = 22, ResultDetails = new List<ResultDetail> { playerResult } };

        var primaryInfo = new OddsInfoModel
        {
            PlayerId = 10,
            TeamId = 1,
            IsHome = true,
            Statistics = new List<Statistic> { new() { Type = "points", Value = "10+" } }
        };
        var primaryOddFlex = new OddFlexModel
        {
            ResourceId = playerResult.ResourceId,
            BetType = "any",
            RelatedBettype = "653,00",
            OddsInfo = JsonConvert.SerializeObject(primaryInfo)
        };
        var odd653 = new OddFlexModel
        {
            ResourceId = "653777",
            OddsInfo = JsonConvert.SerializeObject(new OddsInfoModel { PlayerId = 10, TeamId = 1 })
        };
        _repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>())
            .Returns(Task.FromResult<IEnumerable<OddFlexModel>>(new[] { primaryOddFlex, odd653 }));

        // Return only one of the required related results to force invalid
        var relatedResults = new List<ResultDetail>
        {
            new() { ResourceId = "653777", SettlementStatusId =4 }
        };
        _repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(relatedResults));

        await Assert.ThrowsAsync<ServiceException>(async () => await _sut.HandleCondition(match));

        Assert.Empty(match.ResultDetails); // filtered because invalid
    }

    [Fact]
    public async Task HandleCondition_OddFlexCached_AvoidsSecondRepositoryCall()
    {
        var playerResult = new ResultDetail { ResourceId = "66912345" };
        var match = new Match { MatchId = 33, ResultDetails = new List<ResultDetail> { playerResult } };

        var info = new OddsInfoModel { PlayerId = 1, TeamId = 1, Statistics = new List<Statistic>() };
        var primaryOddFlex = new OddFlexModel
        {
            ResourceId = playerResult.ResourceId,
            BetType = "any",
            RelatedBettype = "00",
            OddsInfo = JsonConvert.SerializeObject(info)
        };
        _repository.FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>())
            .Returns(Task.FromResult<IEnumerable<OddFlexModel>>(new[] { primaryOddFlex }));
        _repository.FetchAsync<ResultDetail>(Arg.Any<CheckRelatedResultsCriteria>())
            .Returns(Task.FromResult<IEnumerable<ResultDetail>>(new[] { new ResultDetail { ResourceId = "00", SettlementStatusId = 4 } }));

        await _sut.HandleCondition(match);
        await _sut.HandleCondition(match); // second call should hit cache for oddflex

        await _repository.Received(1).FetchAsync<OddFlexModel>(Arg.Any<GetPlayerInfoCriteria>());
    }
}
