using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Constants;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTest.Services;

public class ScoreServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly WriteLogService writeLogService;

    private readonly UpdateResultStatusService updateResultStatusService;

    private readonly ScoreService service;

    public ScoreServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        writeLogService = Substitute.For<WriteLogService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        service = new ScoreServiceImp(repository, writeLogService, updateResultStatusService);
    }

    [Fact]
    public async Task GetScore_Always_FetchData()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new()
                {
                    ResourceId = "01"
                },
                new()
                {
                ResourceId = "02"
            }
            }
        };

        await service.GetScore(match);

        await repository.Received().FetchAsync<Score>(Arg.Is<GetConfirmScoreCriteria>(c => c.MatchId == 1 && c.ResourceIds == "01,02"));
    }

    [Fact]
    public void WriteLogResultInvalid_SourceHomeValueOnly_GenerateMessageBaseOnScoreAndExecute()
    {
        var resultDetail = new ResultDetail
        {
            ResourceId = "01"
        };

        var scores = new List<Score>
        {
            new ()
            {
                SourceId = 0,
                SourceName = "Trader",
                HomeValue = "1"
            },
            new ()
            {
                SourceId = 1,
                SourceName = "FirstProvider",
                HomeValue = "2"
            },
            new ()
            {
                SourceId = 2,
                SourceName = "SecondProvider",
                HomeValue = "2"
            },
        };

        service.WriteLogResultInvalid(1, resultDetail, scores);

        writeLogService.Received().WriteLog(
                                Arg.Is<Match>(m => m.MatchId == 1),
                                Arg.Is<string>(m => m == "Failed: Result(01) values inconsistent between Trader(1), FirstProvider(2), SecondProvider(2)"));
    }

    [Fact]
    public void WriteLogResultInvalid_SourceHomeAwayValue_GenerateMessageBaseOnScoreAndExecute()
    {
        var resultDetail = new ResultDetail
        {
            ResourceId = "01"
        };

        var scores = new List<Score>
        {
            new ()
            {
                SourceId = 0,
                SourceName = "Trader",
                HomeValue = "1",
                AwayValue = "2"
            },
            new ()
            {
                SourceId = 1,
                SourceName = "FirstProvider",
                HomeValue = "2",
                AwayValue = "2"
            },
            new ()
            {
                SourceId = 2,
                SourceName = "SecondProvider",
                HomeValue = "2",
                AwayValue = "3"
            },
        };

        service.WriteLogResultInvalid(1, resultDetail, scores);

        writeLogService.Received().WriteLog(
            Arg.Is<Match>(m => m.MatchId == 1),
            Arg.Is<string>(m => m == "Failed: Result(01) values inconsistent between Trader(1-2), FirstProvider(2-2), SecondProvider(2-3)"));
    }

    [Fact]
    public async Task UpdateResultStatus_Always_CallUpdateStatusService()
    {
        var resultDetails = new List<ResultDetail>
        {
            new()
            {
                ResourceId = "01"
            }
        };

        await service.UpdateResultStatus(1, resultDetails);

        await updateResultStatusService.Received().UpdateResultStatus(Arg.Is<Match>(m => m.MatchId == 1
             && m.ResultDetails.Count() == 1
             && m.ResultDetails.First().ResourceId == "01"), MatchStatus.Failed);
    }
}