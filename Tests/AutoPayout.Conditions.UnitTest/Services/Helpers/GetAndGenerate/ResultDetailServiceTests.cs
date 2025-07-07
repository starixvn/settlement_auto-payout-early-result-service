using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using AutoPayout.Services;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers.GetAndGenerate;

public class ResultDetailServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly WriteLogService writeLogService;
    private readonly UpdateResultStatusService updateResultStatusService;
    private readonly ResultDetailServiceImp service;

    public ResultDetailServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        writeLogService = Substitute.For<WriteLogService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        service = new ResultDetailServiceImp(repository, writeLogService, updateResultStatusService);
    }

    [Fact]
    public async Task GetScores_ReturnsScoresFromRepository()
    {
        var match = new Match { MatchId = 1, SportType = 2, ResultDetails = new List<ResultDetail> { new() { ResourceId = "01" } } };
        var expectedScores = new List<Score> { new() { MatchId = 1, ResourceId = "01" } };
        repository.FetchAsync<Score>(Arg.Any<GetConfirmScoreCriteria>()).Returns(expectedScores);

        var result = await service.GetScores(match);

        Assert.Single(result);
        Assert.Equal("01", result.First().ResourceId);
    }

    [Fact]
    public async Task GetSingleScore_ReturnsScoresFromRepository()
    {
        var resultDetail = new ResultDetail { MatchId = 1, SportType = 2, ResourceId = "01" };
        var expectedScores = new List<Score> { new() { MatchId = 1, ResourceId = "01" } };
        repository.FetchAsync<Score>(Arg.Any<GetConfirmScoreCriteria>()).Returns(expectedScores);

        var result = await service.GetSingleScore(resultDetail);

        Assert.Single(result);
        Assert.Equal("01", result.First().ResourceId);
    }

    [Fact]
    public void WriteLogResultNotTrader_WritesLogAndReturnsMessage()
    {
        var result = new ResultDetail { ResourceId = "01" };

        var message = service.WriteLogResultNotTrader(1, result);

        Assert.Contains("01", message);
        writeLogService.Received().WriteLog(Arg.Any<Match>(), message);
    }

    [Fact]
    public void WriteLogResult_WritesLog()
    {
        var result = new ResultDetail { ResourceId = "01" };

        service.WriteLogResult(1, result, "msg");

        writeLogService.Received().WriteLog(Arg.Any<Match>(), "msg");
    }

    [Fact]
    public void WriteLogResults_WritesLog()
    {
        var results = new List<ResultDetail> { new() { ResourceId = "01" } };

        service.WriteLogResults(1, results, "msg");

        writeLogService.Received().WriteLog(Arg.Any<Match>(), "msg");
    }

    [Fact]
    public async Task UpdateInvalidResultStatus_WithResults_CallsUpdateResultStatus()
    {
        var resultDetails = new List<ResultDetail> { new() { ResourceId = "01" } };

        await service.UpdateInvalidResultStatus(1, resultDetails, "remark");

        await updateResultStatusService.Received().UpdateResultStatus(Arg.Any<Match>(), MatchStatus.Failed, "remark");
    }

    [Fact]
    public async Task UpdateInvalidResultStatus_WithoutResults_DoesNotCallUpdateResultStatus()
    {
        var resultDetails = new List<ResultDetail>();

        await service.UpdateInvalidResultStatus(1, resultDetails, "remark");

        await updateResultStatusService.DidNotReceive().UpdateResultStatus(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<string>());
    }



    [Fact]
    public async Task GetResourceIndex_ReturnsFilteredIndexes()
    {
        var indexes = new List<ResourceIndex> {
            new() { ResourceId = "01" },
            new() { ResourceId = ResourceIds.FullTime }
        };

        repository.FetchAsync<ResourceIndex>(Arg.Any<GetResourceIndexCriteria>()).Returns(indexes);
        var result = await service.GetResourceIndex(1);

        Assert.Single(result);
        Assert.Equal("01", result.First().ResourceId);
    }

    [Fact]
    public async Task GetResultsChecking_ReturnsExpected()
    {
        var match = new Match { SportType = 1, ResultDetails = new List<ResultDetail> { new() { ResourceId = "01" } } };
        var settled = new List<ResultDetail> { new() { ResourceId = "02" } };
        var indexes = new List<ResourceIndex> { new() { ResourceId = "01", IntervalNo = 1 }, new() { ResourceId = "02", IntervalNo = 2 } };
        repository.FetchAsync<ResourceIndex>(Arg.Any<GetResourceIndexCriteria>()).Returns(indexes);

        var (results, idxs) = await service.GetResultsChecking(match, 2, settled);

        Assert.Equal(2, results.Count);
        Assert.Equal(2, idxs.Count);
    }


    [Fact]
    public void GetValidSetResults_ReturnsExpected()
    {
        var match = new Match { ResultDetails = new List<ResultDetail> { new() { ResourceId = "01" } } };
        var settled = new List<ResultDetail> { new() { ResourceId = "02" } };
        var indexes = new List<ResourceIndex> { new() { ResourceId = "01", IntervalNo = 1 }, new() { ResourceId = "02", IntervalNo = 2 } };

        var result = service.GetValidSetResults(match, settled, indexes, 2);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ResourceId == "01");
        Assert.Contains(result, r => r.ResourceId == "02");
    }

    [Fact]
    public void WriteLogResultInvalid_Always_WriteCorrectMessage()
    {
        var expectedMessage = "Failed Auto Payout: Result(01) values inconsistent between Trader(2-2), SBO(1-2), SEO(2-2)";

        var scores = new List<Score>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceName = "SBO",
                SourceId = 1
            },
            new ()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceName = "SEO",
                SourceId = 2
            },
            new ()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceName = "SEO"
            },
        };

        service.WriteLogResultInvalid(1, new ResultDetail() { ResourceId = "01" }, scores);

        writeLogService.Received().WriteLog(Arg.Any<Match>(), expectedMessage);
    }

    [Fact]
    public void WriteLogResultInvalid_ProviderOnly_WriteCorrectMessage()
    {
        var expectedMessage = "Failed Auto Payout: Result(01) values inconsistent between SBO(1-2), SEO(2-2)";

        var scores = new List<Score>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = "1",
                AwayValue = "2",
                SourceName = "SBO",
                SourceId = 1
            },
            new ()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceName = "SEO",
                SourceId = 2 },
        };

        service.WriteLogResultInvalid(1, new ResultDetail() { ResourceId = "01" }, scores);

        writeLogService.Received().WriteLog(Arg.Any<Match>(), expectedMessage);
    }

    [Fact]
    public void WriteLogResultInvalid_TraderOnly_WriteCorrectMessage()
    {
        var expectedMessage = "Failed Auto Payout: Result(01) values inconsistent between Trader(2-2)";

        var scores = new List<Score>()
        {
            new ()
            {
                ResourceId = "01",
                HomeValue = "2",
                AwayValue = "2",
                SourceName = "SEO"
            },
        };

        service.WriteLogResultInvalid(1, new ResultDetail() { ResourceId = "01" }, scores);

        writeLogService.Received().WriteLog(Arg.Any<Match>(), expectedMessage);
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

        await service.UpdateInvalidResultStatus(1, resultDetails);

        await updateResultStatusService.Received().UpdateResultStatus(Arg.Is<Match>(m => m.MatchId == 1
            && m.ResultDetails.Count() == 1
            && m.ResultDetails.First().ResourceId == "01"), MatchStatus.Failed);
    }
}
