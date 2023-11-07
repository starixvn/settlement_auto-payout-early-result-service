using AutoPayout.Criteria;
using AutoPayout.Exceptions;
using AutoPayout.Models;
using Fanex.Data.Repository;

namespace AutoPayout.UnitTests.Services;

public class WriteLogServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly WriteLogService service;

    public WriteLogServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        service = new WriteLogServiceImp(repository);
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
            .Execute(Arg.Is<WriteLogCriteria>(c => c.MatchId == 1 && c.Remark == "NAP: Auto Payout: 01<br/>Message"));
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
            .Execute(Arg.Is<WriteLogCriteria>(c => c.MatchId == 1 && c.Remark == "NAP: Auto Payout: 01<br/>Message"));
    }

    [Fact]
    public void WriteLogDirectly_Always_ExecuteCriteria()
    {
        service.WriteLogDirectly(1, "Message");

        repository.Received()
            .Execute(Arg.Is<LogDirectlyCriteria>(c => c.Extend == 1 && c.Doing == "Message"));
    }

    [Fact]
    public void InsertResettleLog_IsMainMatch_ExecuteCriteria()
    {
        service.InsertResettleLog(new SettleCriteria(), 1, true);

        repository.Received().Execute(Arg.Any<InsertLogCriteria>());
    }

    [Fact]
    public void InsertResettleLog_IsNotMainMatch_ExecuteCriteria()
    {
        service.InsertResettleLog(new SettleCriteria(), 1, false);

        repository.DidNotReceive().Execute(Arg.Any<InsertLogCriteria>());
    }
}