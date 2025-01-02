using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Models;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalWithExtraResultTests
{
    private readonly ResultDetailService mockResultDetailService;
    private readonly ExtraResultService mockExtraResultService;
    private readonly VerifyFinalWithExtraResult verifyFinalWithExtraResult;

    public VerifyFinalWithExtraResultTests()
    {
        mockResultDetailService = Substitute.For<ResultDetailService>();
        mockRepository = Substitute.For<IDynamicRepository>();
        var mockOptions = Substitute.For<IOptions<AppSettingsKey>>();
        mockExtraResultService = Substitute.For<ExtraResultService>();

        var appSettings = new AppSettingsKey { RetryProcessScore = 1000 };
        mockOptions.Value.Returns(appSettings);

        verifyFinalWithExtraResult = new VerifyFinalWithExtraResult(
            mockResultDetailService,
            mockOptions,
            mockExtraResultService
        );
    }

    [Fact]
    public async Task HandleCondition_ShouldSkipFinalResultCheck_WhenNotMainResource()
    {
        // Arrange
        var match = new Match
        {
            ResourceType = ResourceType.EarlyResource,
            SportType = SportType.Soccer,
            ResultDetails = new List<ResultDetail>
        {
            new ResultDetail { ResourceId = ResourceIds.FullTime }
        }
        };

        // Act
        await verifyFinalWithExtraResult.HandleCondition(match);

        // Assert
        await mockResultDetailService.DidNotReceiveWithAnyArgs().GetResultSettled(Arg.Any<int>(), Arg.Any<int>());
    }


    [Fact]
    public async Task HandleCondition_ShouldHandleInvalidFinalResult_WhenErrorMessageReturned()
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime };
        var matchDetail = new MatchDetail { SessionTime = 2 };

        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            SportType = SportType.BaseBall,
            ResultDetails = new List<ResultDetail> { finalResult },
            MatchDetail = matchDetail
        };


        mockResultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>())
                               .Returns(new List<ResultDetail>());

        mockResultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>())
                       .Returns((new List<ResultDetail>(), new List<ResourceIndex>()));

        mockExtraResultService.GetScoreExtraInning(match, Arg.Any<int>(), Arg.Any<int>()).Returns(new ResultDetail { ResourceId = "EI", IsValid = false });

        mockResultDetailService.MaxTimeScore(Arg.Any<ResultDetail>())
                               .Returns(1500);

        // Act
        await verifyFinalWithExtraResult.HandleCondition(match);

        // Assert
        await mockResultDetailService.Received(1)
                                     .UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());

        mockResultDetailService.Received(1)
                                     .WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }


    [Fact]
    public async Task HandleCondition_ShouldHandlInvalidFinalResult_WhenErrorMessageMaxScoreReturned()
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime };
        var matchDetail = new MatchDetail { SessionTime = 2 };

        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            SportType = SportType.BaseBall,
            ResultDetails = new List<ResultDetail> { finalResult },
            MatchDetail = matchDetail
        };

        mockResultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>())
                               .Returns(new List<ResultDetail>());

        mockResultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>())
                       .Returns((new List<ResultDetail> { finalResult }, new List<ResourceIndex>()));

        mockExtraResultService.GetScoreExtraInning(match, Arg.Any<int>(), Arg.Any<int>()).Returns(new ResultDetail { ResourceId = "EI", IsValid = false });

        mockResultDetailService.MaxTimeScore(Arg.Any<ResultDetail>())
                               .Returns(1500);

        // Act
        await verifyFinalWithExtraResult.HandleCondition(match);

        // Assert
        await mockResultDetailService.Received(1)
                                     .UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());

        mockResultDetailService.Received(1)
                                     .WriteLogResult(Arg.Any<int>(), Arg.Any<ResultDetail>(), Arg.Any<string>());
    }


    [Fact]
    public async Task HandleCondition_ShouldHandlInvalidFinalResultNeedRetry_WhenErrorMessageMaxScoreReturned()
    {
        // Arrange
        var finalResult = new ResultDetail { ResourceId = ResourceIds.FullTime };
        var matchDetail = new MatchDetail { SessionTime = 2 };

        var match = new Match
        {
            MatchId = 1,
            ResourceType = ResourceType.MainResource,
            SportType = SportType.BaseBall,
            ResultDetails = new List<ResultDetail> { finalResult },
            MatchDetail = matchDetail
        };


        mockResultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>())
                               .Returns(new List<ResultDetail>());

        mockResultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<List<ResultDetail>>())
                       .Returns((new List<ResultDetail>(), new List<ResourceIndex>()));

        mockExtraResultService.GetScoreExtraInning(match, Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ResultDetail { ResourceId = "EI", IsValid = false, NeedRetry = true });

        mockResultDetailService.MaxTimeScore(Arg.Any<ResultDetail>())
                               .Returns(1500);

        // Act
        await verifyFinalWithExtraResult.HandleCondition(match);

        // Assert
        await mockResultDetailService.DidNotReceive()
                                     .UpdateInvalidResultStatus(Arg.Any<int>(), Arg.Any<List<ResultDetail>>(), Arg.Any<string>());

    }
}



