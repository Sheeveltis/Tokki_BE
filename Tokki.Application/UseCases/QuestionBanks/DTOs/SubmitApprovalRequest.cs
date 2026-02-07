namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class SubmitApprovalRequest
    {
        public List<string> QuestionBankIds { get; set; } = new();
    }

}
