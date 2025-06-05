using System;
using System.Text;
using System.Security.Cryptography;

namespace windowOP
{
    public static class Crypto
    {
        public static (string privateKey, string publicKey) GetRSAKey(int keySize = 1024)
        {
            using (RSA rsa = RSA.Create(keySize))
            {
                // 以 PKCS#8 格式导出私钥
                string privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                // 以 PKCS#1 格式导出公钥
                string publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                return (privateKey, publicKey);
            }
        }

        public static string? RSA_de(string cipherText, string privateKey)
        {
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    // 导入私钥
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

                    // 将 Base64 编码的密文字符串转换为字节数组
                    byte[] encryptedData = Convert.FromBase64String(cipherText);

                    // 解密
                    byte[] decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

                    // 返回解密后的字符串
                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            catch
            {
                DatabaseOP.LogErr("RSA解密出现错误");
                return null;
            }
        }

        public static string? AES_en(string plainText, string key, string iv)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // 设置密钥和初始化向量（IV）
                    aes.Key = GetKey(key);
                    aes.IV = GetIv(iv);

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }
                        byte[] encrypted = msEncrypt.ToArray();
                        return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
                    }
                }
            }
            catch
            {
                DatabaseOP.LogErr("AES加密出现错误");
                return null;
            }
        }

        public static string? AES_de(string cipherText, string key, string iv)
        {
            try
            {
                byte[] cipherBytes = StringToByteArray(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // 设置密钥和初始化向量（IV）
                    aes.Key = GetKey(key);
                    aes.IV = GetIv(iv);

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(csDecrypt))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                DatabaseOP.LogErr("AES解密出现错误");
                return null;
            }
        }

        private static byte[] GetKey(string key)
        {
            // 使用UTF8编码将字符串密钥转换为字节数组，确保长度为16字节
            return Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));
        }

        private static byte[] GetIv(string iv)
        {
            // 使用UTF8编码将字符串IV转换为16字节数组
            return Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
        }

        private static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }


        public static string HmacSha256(string data, string key)
        {
            // 将密钥和数据转换为字节数组
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            // 使用 HMACSHA256 进行哈希
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                // 计算 HMAC
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                // 将哈希字节数组转换为十六进制字符串
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

    }
}