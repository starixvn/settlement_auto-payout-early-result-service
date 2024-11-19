using AutoPayout.Conditions.Criteria;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyBasketBallFinalResultTests
{
    private readonly ResultDetailService resultDetailService;

    private readonly IDynamicRepository repository;

    private readonly BasketballHelperService basketballHelper;

    private readonly VerifyBasketBallResult service;

    private readonly ResultDetail halfTime = new() { ResourceId = ResourceIds.Halftime };

    private readonly ResultDetail fullTime = new() { ResourceId = ResourceIds.FullTime };

    public VerifyBasketBallFinalResultTests()
    {
        resultDetailService = Substitute.For<ResultDetailService>();
        repository = Substitute.For<IDynamicRepository>();
        basketballHelper = Substitute.For<BasketballHelperService>();
        service = new VerifyBasketBallResult(resultDetailService, repository, basketballHelper);
    }

    [Fact]
    public async Task HandleCondition_NotExistResultFinal_OnlyUpdateMatchDetail()
    {
        var match = new Match()
        {
            ResultDetails = []
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 2 });

        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<IEnumerable<ResultDetail>>())
            .Returns(([new ResultDetail { ResourceId = "Q1" }], []));

        await service.HandleCondition(match);

        Assert.Equal(2, match.MatchDetail.GameSession);
        await basketballHelper.DidNotReceive().VerifyHalfTimeInValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<List<ResultDetail>>(),
            Arg.Any<int>());

        await basketballHelper.DidNotReceive()
             .FinalOfBo2InValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<ResultDetail>());
    }

    [Fact]
    public async Task HandleCondition_HTExistAndInValid_UpdateInvalidResult()
    {
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = [halfTime]
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 2 });

        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<IEnumerable<ResultDetail>>())
            .Returns(([], []));
        basketballHelper.VerifyHalfTimeInValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<List<ResultDetail>>(),
            Arg.Any<int>()).Returns("Error");

        await service.HandleCondition(match);
        await resultDetailService.Received(1).UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.Received(1).WriteLogResult(1, Arg.Any<ResultDetail>(), Arg.Any<string>());
        await basketballHelper.DidNotReceive()
            .FinalOfBo2InValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<ResultDetail>());
        Assert.Empty(match.ResultDetails);
    }

    [Fact]
    public async Task HandleCondition_BothFTAndHTExistButInValid_UpdateInvalidResults()
    {
        var match = new Match()
        {
            MatchId = 1,
            ResultDetails = [halfTime, fullTime]
        };
        repository.GetAsync<MatchDetail>(Arg.Any<GetMatchDetailCriteria>())
            .Returns(new MatchDetail() { GameSession = 2 });

        resultDetailService.GetResultSettled(Arg.Any<int>(), Arg.Any<int>()).Returns([]);
        resultDetailService.GetResultsChecking(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<IEnumerable<ResultDetail>>())
            .Returns(([], []));
        basketballHelper.VerifyHalfTimeInValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<List<ResultDetail>>(),
            Arg.Any<int>()).Returns("Error");
        basketballHelper.FinalOfBo2InValid(Arg.Any<Match>(), Arg.Any<ResultDetail>(), Arg.Any<ResultDetail>()).Returns("true");

        await service.HandleCondition(match);
        await resultDetailService.Received(2).UpdateInvalidResultStatus(1, Arg.Any<List<ResultDetail>>(), Arg.Any<string>());
        resultDetailService.Received(2).WriteLogResult(1, Arg.Any<ResultDetail>(), Arg.Any<string>());
        Assert.Empty(match.ResultDetails);
    }
}