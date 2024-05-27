using AutoPayout.Conditions.Constants;
using AutoPayout.Criteria;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services.ExtraStepServiceTests.AfterSettleTests;
public class BasketBallCashOutTests
{
    private readonly IHttpClientFactory httpClient;

    private readonly BasketBallCashOut service;

    private readonly HttpClient client;

    private readonly AppSettingsKey appSettingsKey;

    public BasketBallCashOutTests()
    {
        httpClient = Substitute.For<IHttpClientFactory>();
        client = Substitute.For<HttpClient>();
        appSettingsKey = new AppSettingsKey()
        {
            ThirdPartyUri = new ThirdPartySettingKeys()
            {
                JackTimeResult = "https://www.google.com/",
                JackTimeTicket = "https://www.google.com/"
            }
        };
        var options = Options.Create(appSettingsKey);
        service = new BasketBallCashOut(options, httpClient);
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
        httpClient.CreateClient().Returns(client);

        await service.Process(match, criteria);

        await client.Received(1).SendAsync(
            Arg.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri == new Uri(appSettingsKey.ThirdPartyUri.JackTimeResult) &&
                req.Content.ReadAsStringAsync().Result.Contains("{\"MatchId\":0,\"BetStatus\":\"1-1,1-2\",\"LsHalf\":[1]}")),
            Arg.Any<CancellationToken>());
    }
}
