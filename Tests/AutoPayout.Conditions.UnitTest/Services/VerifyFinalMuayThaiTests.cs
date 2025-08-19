using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using AutoPayout.Services;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.UnitTests.Services;

public class VerifyFinalMuayThaiTests
{
    private readonly ResultDetailService resultDetailService;
    private readonly MuayThaiHelperService muayThaiHelperService;
    private readonly UpdateResultStatusService updateResultStatusService;
    private readonly VerifyFinalMuayThai condition;

    public VerifyFinalMuayThaiTests()
    {
        resultDetailService = Substitute.For<ResultDetailService>();
        muayThaiHelperService = Substitute.For<MuayThaiHelperService>();
        updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        var scoreMessageService = Substitute.For<ScoreMessageService>();
        var appSettings = new AppSettingsKey { RetryProcessScore = 300000 };
        var options = Options.Create(appSettings);
        condition = new VerifyFinalMuayThai(resultDetailService, muayThaiHelperService, updateResultStatusService, scoreMessageService, options);
    }

    [Fact]
    public async Task HandleCondition_SingleScoreWithSourceId_MapsCorrectly()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Single(resultDetail.ExternalSourceInfo);
    }

    [Fact]
    public async Task HandleCondition_SingleScoreWithoutSourceId_KeepsOriginalValues()
    {
        var match = CreateMatch();
        match.MatchDetail.BetStatus = "3";
        var scores = new List<Score>
        {
            new Score { SourceId = 0, ResourceId = "00", HomeValue = "1", AwayValue = "0", BetStatus = "3", SourceName = "Trader" }
        };

        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("3", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Single(resultDetail.ExternalSourceInfo);
    }

    [Fact]
    public async Task HandleCondition_MultipleConsistentScores_ProcessesSuccessfully()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1" },
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO2" },
            new Score { SourceId = 0, ResourceId = "00", HomeValue = "1", AwayValue = "0", BetStatus = "4", SourceName = "Trader" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Equal(3, resultDetail.ExternalSourceInfo.Count());
    }

    [Fact]
    public async Task HandleCondition_MultipleInconsistentScores_MarksAsInvalid()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1", CreateDate = DateTime.Now},
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "5", AwayValue = "0", SourceName = "SBO2", CreateDate = DateTime.Now }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        muayThaiHelperService.MapSboToSettle(5, 0).Returns(("1", "0", "5", true));
        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_ExpiredScores_UpdatesInvalidResultStatus()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1", CreateDate = DateTime.Now.AddMinutes(-10) },
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "5", AwayValue = "0", SourceName = "SBO2", CreateDate = DateTime.Now.AddMinutes(-10) }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        muayThaiHelperService.MapSboToSettle(5, 0).Returns(("1", "0", "5", true));
        resultDetailService.GetScores(match).Returns(scores);

        await updateResultStatusService.UpdateResultStatus(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<string>());

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_NoScores_HandlesGracefully()
    {
        var match = CreateMatch();

        resultDetailService.GetScores(match).Returns(new List<Score>());

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.True(resultDetail.IsValid);
        Assert.Empty(resultDetail.ExternalSourceInfo);
    }

    [Fact]
    public async Task HandleCondition_InvalidScoreFormat_HandlesGracefully()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "invalid", AwayValue = "0", SourceName = "SBO", CreateDate = DateTime.Now }
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_SingleScoreWithInvalidMapping_ReturnsOriginalValues()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO", CreateDate  = DateTime.Now}
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("4", "0", "", false));
        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_MultipleScoresWithSomeInvalidMapping_ProcessesValidOnes()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1" },
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "invalid", AwayValue = "0", SourceName = "SBO2" },
            new Score { SourceId = 3, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO3" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Equal(3, resultDetail.ExternalSourceInfo.Count());
    }

    [Fact]
    public async Task HandleCondition_AllScoresInvalidMapping_ReturnEmptyResultDetail()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "invalid1", AwayValue = "0", SourceName = "SBO1", CreateDate = DateTime.Now },
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "invalid2", AwayValue = "0", SourceName = "SBO2", CreateDate = DateTime.Now }
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_NonExpiredInconsistentScores_DoesNotUpdateResultStatus()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1", CreateDate = DateTime.Now.AddMinutes(-6) },
            new Score { SourceId = 2, ResourceId = "00", HomeValue = "5", AwayValue = "0", SourceName = "SBO2", CreateDate = DateTime.Now }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        muayThaiHelperService.MapSboToSettle(5, 0).Returns(("1", "0", "5", true));
        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
        await updateResultStatusService.DidNotReceive().UpdateResultStatus(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_EmptyScoreList_ReturnsEarly()
    {
        var match = CreateMatch();
        resultDetailService.GetScores(match).Returns(new List<Score>());

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.True(resultDetail.IsValid);
        Assert.Empty(resultDetail.ExternalSourceInfo);
        await updateResultStatusService.DidNotReceive().UpdateResultStatus(Arg.Any<Match>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleCondition_SingleScoreWithNullValues_HandlesGracefully()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = null, AwayValue = null, SourceName = "SBO", CreateDate = DateTime.Now }
        };

        resultDetailService.GetScores(match).Returns(scores);

        var exception = await Assert.ThrowsAsync<ServiceException>(() => condition.HandleCondition(match));
        Assert.False(exception.MarkFail);
    }

    [Fact]
    public async Task HandleCondition_MixedSourceIds_ProcessesCorrectly()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 0, ResourceId = "00", HomeValue = "1", AwayValue = "0", BetStatus = "3", SourceName = "Trader" },
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Equal(2, resultDetail.ExternalSourceInfo.Count());
    }

    [Fact]
    public async Task HandleCondition_ExternalSourceInfoMapping_IsCorrect()
    {
        var match = CreateMatch();
        var scores = new List<Score>
        {
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO1" },
            new Score { SourceId = 2, ResourceId = "01", HomeValue = "4", AwayValue = "0", SourceName = "SBO2" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        var externalSources = resultDetail.ExternalSourceInfo.ToList();

        Assert.Equal(2, externalSources.Count);
        Assert.Equal("SBO1", externalSources[0].SourceName);
        Assert.Equal("00", externalSources[0].ResourceId);
        Assert.Equal(1, externalSources[0].SourceId);
        Assert.Equal("SBO2", externalSources[1].SourceName);
        Assert.Equal("01", externalSources[1].ResourceId);
        Assert.Equal(2, externalSources[1].SourceId);
    }

    [Fact]
    public async Task HandleCondition_SourceIdZero_SetsBetStatusFromMatchDetail()
    {
        var match = CreateMatch();
        match.MatchDetail.BetStatus = "5";
        var scores = new List<Score>
        {
            new Score { SourceId = 0, ResourceId = "00", HomeValue = "1", AwayValue = "0", SourceName = "Trader" },
            new Score { SourceId = 1, ResourceId = "00", HomeValue = "4", AwayValue = "0", SourceName = "SBO" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Equal(2, resultDetail.ExternalSourceInfo.Count());
    }

    [Fact]
    public async Task HandleCondition_MultipleSourceIdZero_AllSetBetStatusFromMatchDetail()
    {
        var match = CreateMatch();
        match.MatchDetail.BetStatus = "3";
        var scores = new List<Score>
        {
            new Score { SourceId = 0, ResourceId = "00", HomeValue = "1", AwayValue = "0", SourceName = "Trader1" },
            new Score { SourceId = 0, ResourceId = "01", HomeValue = "1", AwayValue = "0", SourceName = "Trader2" },
            new Score { SourceId = 1, ResourceId = "02", HomeValue = "4", AwayValue = "0", SourceName = "SBO" }
        };

        muayThaiHelperService.MapSboToSettle(4, 0).Returns(("1", "0", "4", true));
        resultDetailService.GetScores(match).Returns(scores);

        await condition.HandleCondition(match);

        var resultDetail = match.ResultDetails.First();
        Assert.Equal("1", resultDetail.HomeValue);
        Assert.Equal("0", resultDetail.AwayValue);
        Assert.Equal("4", match.MatchDetail.BetStatus);
        Assert.True(resultDetail.IsValid);
        Assert.Equal(3, resultDetail.ExternalSourceInfo.Count());
    }

    private static Match CreateMatch()
    {
        return new Match
        {
            MatchId = 123,
            ResultDetails = [new ResultDetail { ResourceId = "00", IsValid = true }],
            MatchDetail = new MatchDetail()
        };
    }
}