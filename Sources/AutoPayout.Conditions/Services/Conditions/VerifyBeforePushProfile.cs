using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyBeforePushProfile(ScaleOutSettleService scaleOutSettleService,
        XmlConvertService xmlConvertService)
    : ICondition
{
    private const string SpProfile = "NAP_Admin_Settlement_Profile_Push";

    private const string Action = "CAL";

    private const string Mode = "ALL";

    public async Task HandleCondition(Match match)
    {
        var request = new EventTicketRequest
        {
            MatchId = match.MatchId,
            Action = Action,
            SpName = SpProfile,
            Mode = Mode,
            SelectedResources = CreateXmlResources(match.ResultDetails)
        };

        Console.WriteLine("VerifyBeforePushProfile");

        var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);

        if (resultMessage.ErrorCode > 0)
        {
            throw resultMessage.ErrorCode switch
            {
                ErrorCode.DeploymentError => new ServiceException { MarkFail = false },
                ErrorCode.MatchInQueue => new ServiceException(ErrorMessage.MatchInQueue) { MarkFail = false },
                _ => new ServiceException(resultMessage.ErrorMessage)
            };
        }
    }

    private string CreateXmlResources(IEnumerable<ResultDetail> selectedResources)
    {
        var resources = new Resources<ResultDetail>
        {
            ResourceItems = selectedResources.ToList()
        };

        return xmlConvertService.Serialize(resources);
    }
}