using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services;

public class VerifyBeforePushProfile : ICondition
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly XmlConvertService xmlConvertService;

    private const string SpProfile = "NAP_Admin_Settlement_Profile_Push";

    private const string Action = "CAL";

    private const string Mode = "ALL";

    public VerifyBeforePushProfile(
        ScaleOutSettleService scaleOutSettleService,
        XmlConvertService xmlConvertService)

    {
        this.xmlConvertService = xmlConvertService;
        this.scaleOutSettleService = scaleOutSettleService;
    }

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

        var resultMessage = await scaleOutSettleService.ValidateEventAndTicket(request);

        if (resultMessage.ErrorCode > 0)
        {
            throw resultMessage.ErrorCode switch
            {
                ErrorCode.DeploymentError => new ServiceException { MarkFail = false },
                ErrorCode.MatchInQueue => new ServiceException(ErrorMessage.MatchInQueue) { NeedRetry = true },
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