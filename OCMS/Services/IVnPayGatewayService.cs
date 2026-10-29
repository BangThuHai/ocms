using BusinessObjects.Entities;

namespace Services;

public interface IVnPayGatewayService
{
    string CreatePaymentUrl(Payment payment, string clientIp, string returnUrl);
    bool IsValidSignature(IEnumerable<KeyValuePair<string, string>> parameters, string? providedHash);
}
