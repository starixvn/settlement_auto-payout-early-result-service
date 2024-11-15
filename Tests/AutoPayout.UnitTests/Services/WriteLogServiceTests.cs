using AutoPayout.Criteria;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.UnitTests.Services;

public class WriteLogServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly WriteLogService service;

    public WriteLogServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        var appSetting = new AppSettingsKey { GroupTest = 0 };
        var option = Options.Create(appSetting);
        service = new WriteLogServiceImp(repository, option);
    }

    [Fact]
    public void WriteLog_WithException_GenerateMessageAndExecute()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
             {
                 new ()
                 {
                     ResourceId = "01"
                 }
             }
        };

        var exception = new ServiceException("Message");

        service.WriteLog(match, exception);

        repository.Received()
            .ExecuteAsync(Arg.Is<WriteLogCriteria>(c => c.MatchId == 1 && c.Remark == "NAP: Auto Payout: 01<br/>Message"));
    }

    [Fact]
    public async Task WriteLog_ExistGroupTestAndException_GenerateAnotherMessageAndExecute()
    {
        var appSetting = new AppSettingsKey { GroupTest = 1 };
        var option = Options.Create(appSetting);
        var tempService = new WriteLogServiceImp(repository, option);
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = "01"
                }
            }
        };

        var exception = new ServiceException("Message");

        await tempService.WriteLog(match, exception);

        await repository.Received()
            .ExecuteAsync(Arg.Is<WriteLogTestCriteria>(c => c.Resources == "[{\"m\":1,\"r\":\"01\",\"s\":3,\"rm\":\"Message\",\"u\":\"123\"}]"));
    }

    [Fact]
    public void WriteLog_Message_GenerateMessageAndExecute()
    {
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = "01"
                }
            }
        };

        service.WriteLog(match, "Message");

        repository.Received()
            .ExecuteAsync(Arg.Is<WriteLogCriteria>(c => c.MatchId == 1 && c.Remark == "NAP: Auto Payout: 01<br/>Message"));
    }

    [Fact]
    public async Task WriteLog_ExistGroupTest_GenerateAnotherMessageAndExecute()
    {
        var appSetting = new AppSettingsKey { GroupTest = 1 };
        var option = Options.Create(appSetting);
        var tempService = new WriteLogServiceImp(repository, option);
        var match = new Match
        {
            MatchId = 1,
            ResultDetails = new List<ResultDetail>
            {
                new ()
                {
                    ResourceId = "01"
                }
            }
        };

        await tempService.WriteLog(match, "Message");

        await repository.Received()
            .ExecuteAsync(Arg.Is<WriteLogTestCriteria>(c => c.Resources == "[{\"m\":1,\"r\":\"01\",\"s\":3,\"rm\":\"Message\",\"u\":\"123\"}]"));
    }

    [Fact]
    public void WriteLogDirectly_Always_ExecuteCriteria()
    {
        service.WriteLogDirectly(1, "Message");

        repository.Received()
            .ExecuteAsync(Arg.Is<LogDirectlyCriteria>(c => c.Extend == 1 && c.Doing == "Message"));
    }

    [Fact]
    public void InsertResettleLog_IsMainMatch_ExecuteCriteria()
    {
        service.InsertResettleLog(new SettleCriteria(), 1, true);

        repository.Received().ExecuteAsync(Arg.Any<InsertLogCriteria>());
    }

    [Fact]
    public void InsertResettleLog_IsNotMainMatch_ExecuteCriteria()
    {
        service.InsertResettleLog(new SettleCriteria(), 1, false);

        repository.DidNotReceive().ExecuteAsync(Arg.Any<InsertLogCriteria>());
    }
}