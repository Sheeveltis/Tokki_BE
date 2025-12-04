namespace Tokki.Application.IServices
{
    public interface ISePayService
    {
        string GenerateQrUrl(string paymentId, decimal amount, string description);
    }
}