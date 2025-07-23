using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Models;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalWithBetStatusTests
{
    private readonly ResultDetailService resultDetailServiceSub;
    private readonly VerifyFinalWithBetStatus sut;

    public VerifyFinalWithBetStatusTests()
    {
        resultDetailServiceSub = Substitute.For<ResultDetailService>();
        sut = new VerifyFinalWithBetStatus(resultDetailServiceSub);
    }


    [Fact]
    public async Task HandleCondition_WhenNoFullTimeResult_ShouldSkip()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail(),
            ResultDetails = new List<ResultDetail>()
        };

        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenBetStatusMalformed_ShouldHandleInvalidResult()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = "1-0,2-1,3" },
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" } }
        };

        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }

    [Fact]
    public async Task HandleCondition_WhenBetStatusNull_ShouldHandleInvalidResult()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail { BetStatus = null, LeagueType = 1 },
            ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" } }
        };

        await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));

        await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            ErrorMessage.InconsistentInterval);
    }

    //[Fact]
    //public async Task HandleCondition_WhenHasOtTrueForRegularMatch_ShouldHandleInvalidResult()
    //{
    //    var match = new Match
    //    {
    //        ResourceType = ResourceType.MainResource,
    //        MatchDetail = new MatchDetail { LeagueType = 1, BetStatus = "1-0,2-1,3-2," },
    //        ResultDetails = new List<ResultDetail> { new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" } }
    //    };

    //    await Assert.ThrowsAsync<ServiceException>(async () => await sut.HandleCondition(match));


    //    await resultDetailServiceSub.Received(1).UpdateInvalidResultStatus(
    //        Arg.Any<int>(),
    //        Arg.Any<IList<ResultDetail>>(),
    //        ErrorMessage.InconsistentInterval);
    //}

    [Theory]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(22)]
    [InlineData(23)]
    [InlineData(24)]
    public async Task HandleCondition_WhenValidChildMatchType_ShouldProcess(short childMatchType)
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                ChildMatchType = childMatchType,
                BetStatus = "1-0,2-1,3-2"
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            }
        };
        await sut.HandleCondition(match);
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenInvalidChildMatchTypeWithoutBetStatus_ShouldNotProcess()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                ChildMatchType = 99,
                BetStatus = ""
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            }
        };
        await sut.HandleCondition(match);
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenParentMatchWithOT_ShouldCheckAllSets()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 1,
                LeagueType = 2,
                BetStatus = "1-0,2-1,3-2,4-3"
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "10", AwayValue = "6" }
            }
        };
        await sut.HandleCondition(match);
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_WhenRegularMatch_ShouldCheckThreeSets()
    {
        var match = new Match
        {
            ResourceType = ResourceType.MainResource,
            MatchDetail = new MatchDetail
            {
                ParentID = 0,
                LeagueType = 1,
                BetStatus = "1-0,2-1,3-2,4-3"
            },
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" }
            }
        };
        await sut.HandleCondition(match);
        await resultDetailServiceSub.DidNotReceive().UpdateInvalidResultStatus(
            Arg.Any<int>(),
            Arg.Any<IList<ResultDetail>>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleInvalidResult_RemovesFullTimeResult()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new() { ResourceId = ResourceIds.FullTime, HomeValue = "6", AwayValue = "3" },
                new() { ResourceId = "other", HomeValue = "1", AwayValue = "1" }
            }
        };
        var finalResult = match.ResultDetails[0];
        var errorMessage = "error";
        var method = typeof(VerifyFinalWithBetStatus).GetMethod("HandleInvalidResult", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var task = (Task)method.Invoke(sut, new object[] { match, finalResult, errorMessage });
        await task;
        Assert.Single(match.ResultDetails);
        Assert.Equal("other", match.ResultDetails[0].ResourceId);
    }
}