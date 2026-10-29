using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using BusinessObjects.Entities;
using Microsoft.Extensions.Configuration;

namespace Services;

public sealed class VnPayGatewayService : IVnPayGatewayService
{
    private readonly IConfiguration _configuration;

    public VnPayGatewayService(IConfiguration configuration) => _configuration = configuration;

    public string CreatePaymentUrl(Payment payment, string clientIp, string returnUrl)
    {
        var baseUrl = Required("BaseUrl");
        var tmnCode = Required("TmnCode");
        var hashSecret = Required("HashSecret");
        if (payment.Id <= 0 || payment.Amount <= 0)
        {
            throw new InvalidOperationException("Khoản đặt cọc chưa hợp lệ.");
        }

        var amountInCents = checked((long)(payment.Amount * 100m));
        var vietnamTime = DateTime.UtcNow.AddHours(7);
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = amountInCents.ToString(CultureInfo.InvariantCulture),
            ["vnp_Command"] = "pay",
            ["vnp_CreateDate"] = vietnamTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = vietnamTime.AddMinutes(10).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = NormalizeIp(clientIp),
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan dat coc lich hen {payment.AppointmentId}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_TxnRef"] = payment.Id.ToString(CultureInfo.InvariantCulture),
            ["vnp_Version"] = _configuration["Vnpay:Version"] ?? "2.1.0"
        };

        // VNPAY uses the sorted, URL-encoded parameter string for HMACSHA512.
        var hashData = BuildQuery(parameters);
        var secureHash = ComputeHmac(hashData, hashSecret);
        parameters["vnp_SecureHashType"] = "HmacSHA512";
        parameters["vnp_SecureHash"] = secureHash;
        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}{BuildQuery(parameters)}";
    }

    public bool IsValidSignature(IEnumerable<KeyValuePair<string, string>> parameters, string? providedHash)
    {
        var hashSecret = _configuration["Vnpay:HashSecret"];
        if (string.IsNullOrWhiteSpace(hashSecret) || string.IsNullOrWhiteSpace(providedHash) || !Regex.IsMatch(providedHash, "\\A[0-9a-fA-F]{128}\\z"))
        {
            return false;
        }

        var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in parameters)
        {
            if (pair.Key.StartsWith("vnp_", StringComparison.Ordinal) &&
                pair.Key is not "vnp_SecureHash" and not "vnp_SecureHashType")
            {
                data[pair.Key] = pair.Value;
            }
        }

        var expected = Convert.FromHexString(ComputeHmac(BuildQuery(data), hashSecret));
        var supplied = Convert.FromHexString(providedHash);
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }

    private string Required(string key)
    {
        var value = _configuration[$"Vnpay:{key}"];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Chưa cấu hình VNPAY:{key}.")
            : value;
    }

    private static string NormalizeIp(string ip) =>
        string.IsNullOrWhiteSpace(ip) || ip == "::1" ? "127.0.0.1" : ip;

    private static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join("&", parameters.Select(pair =>
            $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value)}"));

    private static string ComputeHmac(string data, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var message = Encoding.UTF8.GetBytes(data);
        return Convert.ToHexString(HMACSHA512.HashData(key, message));
    }
}
