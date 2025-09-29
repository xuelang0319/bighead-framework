namespace Shared;

public static class Protocol
{
    // HTTP Header 约定（全部自定义头，以 X- 开头）
    public const string H_FileName   = "X-File-Name";     // 原始文件名（zip）
    public const string H_Sha256     = "X-Plain-SHA256";  // 明文zip的SHA256（HEX）
    public const string H_Nonce      = "X-Nonce";         // AES-GCM Nonce(Base64)
    public const string H_Tag        = "X-Tag";           // AES-GCM Tag(Base64)
    public const string H_Timestamp  = "X-Timestamp";     // 13位毫秒时间戳
    public const string H_NonceRand  = "X-Req-Nonce";     // 请求随机串（Base64）
    public const string H_Signature  = "X-Signature";     // HMAC-SHA256(Base64)

    // HMAC 参与签名的“认证串”拼装规则（客户端 & 服务器必须一致）
    // message = $"{fileName}|{sha256}|{timestamp}|{reqNonceBase64}|{nonceBase64}|{tagBase64}|{bodyLength}"
    public static string BuildAuthMessage(
        string fileName, string sha256, string timestamp,
        string reqNonceB64, string nonceB64, string tagB64, long bodyLength)
    {
        return $"{fileName}|{sha256}|{timestamp}|{reqNonceB64}|{nonceB64}|{tagB64}|{bodyLength}";
    }
}