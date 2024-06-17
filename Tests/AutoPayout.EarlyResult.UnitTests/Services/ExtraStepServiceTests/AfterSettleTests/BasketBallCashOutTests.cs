using AutoPayout.Conditions.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.Models;
using AutoPayout.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.AfterSettleTests;
public class BasketBallCashOutTests
{
    private readonly IHttpClientFactory httpClient;

    private readonly BasketBallCashOut service;

    private readonly InvokeExternalAPI invokeExternalApi;

    private readonly AppSettingsKey appSettingsKey;

    public BasketBallCashOutTests()
    {
        httpClient = Substitute.For<IHttpClientFactory>();
        invokeExternalApi = Substitute.For<InvokeExternalAPI>();
        appSettingsKey = new AppSettingsKey()
        {
            ThirdPartyUri = new ThirdPartySettingKeys()
            {
                JackTimeResult = "https://www.google.com/",
                JackTimeTicket = "https://www.google.com/"
            }
        };
        var options = Options.Create(appSettingsKey);
        service = new BasketBallCashOut(options, invokeExternalApi);
    }

    [Fact]
    public async Task Process_Always_CallAPIWithCorrectParams()
    {
        var match = new Match()
        {
            ResultDetails = new List<ResultDetail>() { new() { ResourceId = "Q1" } },
            MatchDetail = new MatchDetail() { GameSession = GameType.Bo4 }
        };

        var criteria = new SettleCriteria() { BetStatus = "1-1,1-2" };

        await service.Process(match, criteria);

        await invokeExternalApi.Received().PostAsync(Arg.Is<CashOutCriteria>(c => c.BetStatus == "1-1,1-2" && c.LsHalf.Length == 1), appSettingsKey.ThirdPartyUri.JackTimeResult);
    }
}
