using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
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

        await service.UpdateResultStatus(1, resultDetails);

        await updateResultStatusService.Received().UpdateResultStatus(Arg.Is<Match>(m => m.MatchId == 1
            && m.ResultDetails.Count() == 1
            && m.ResultDetails.First().ResourceId == "01"), MatchStatus.Failed);
    }
}
