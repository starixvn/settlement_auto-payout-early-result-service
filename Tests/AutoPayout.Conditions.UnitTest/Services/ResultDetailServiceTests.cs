using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;
public class ResultDetailServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly WriteLogService writeLogService;

    private readonly UpdateResultStatusService updateResultStatusService;

    private readonly ResultDetailService service;

    public ResultDetailServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        writeLogService = Substitute.For<WriteLogService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        service = new ResultDetailServiceImp(repository, writeLogService, updateResultStatusService);
    }

    [Fact]
    public async Task GetScore_AlwaysSupportAwayValue_ReturnAllScoreHasAwayValueIsTrue()
    {
        var scores = new List<Score>()
        {
            new ()
            {
                HasAwayValue = false
            },

            new ()
            {
            HasAwayValue = true
            }
        };

        repository.FetchAsync<Score>(Arg.Any<GetConfirmScoreCriteria>()).Returns(scores);

        var result = await service.GetScore(new Match() { MatchId = 1, SportType = SportType.Tennis, ResultDetails = new List<ResultDetail>() });

        Assert.Equal(2, result.Count(r => r.HasAwayValue));
    }


    [Fact]
    public async Task GetScore_NonSupportAwayValue_ReturnOriginalValue()
    {
        var scores = new List<Score>()
        {
            new ()
            {
                ResourceId = "00",
                HasAwayValue = false
            },

            new ()
            {
                ResourceId = "01",
                HasAwayValue = true
            }
        };

        repository.FetchAsync<Score>(Arg.Any<GetConfirmScoreCriteria>()).Returns(scores);

        var result = (await service.GetScore(new Match() { MatchId = 1, SportType = SportType.SportMayNotHaveAwayValue[0], ResultDetails = new List<ResultDetail>() })).ToList();

        Assert.True(!result[0].HasAwayValue && result[0].ResourceId == "00");
        Assert.True(result[1].HasAwayValue && result[1].ResourceId == "01");
    }
}
