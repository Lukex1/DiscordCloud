using System.Security.Cryptography;

namespace DiscordCloud.Hash
{
    public class Encryptor
    {
        public byte[] EncryptFile(Stream inputStream, string password)
        {
            byte[] salt = GenerateRandomBytes(16);
            byte[] iv = GenerateRandomBytes(16);

            using var msOutput = new MemoryStream();

            msOutput.Write(salt, 0, salt.Length);
            msOutput.Write(iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = DeriveKey(password, salt);
            aes.IV = iv;

            using var cryptoStream = new CryptoStream(msOutput, aes.CreateEncryptor(), CryptoStreamMode.Write);
            inputStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
            return msOutput.ToArray();
        }

        private byte[] GenerateRandomBytes(int length)
        {
            byte[] data = new byte[length];
            RandomNumberGenerator.Fill(data);
            return data;
        }
        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }
    }

    public class Decryptor
    {
        public byte[] Decrypt(Stream encryptedStream, string password)
        {
            using var msInput = new MemoryStream();
            encryptedStream.CopyTo(msInput);
            byte[] encryptedData = msInput.ToArray();

            byte[] salt = encryptedData[..16];
            byte[] iv = encryptedData[16..32];
            byte[] cipherText = encryptedData[32..];

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = DeriveKey(password, salt);
            aes.IV = iv;

            using var msOutput = new MemoryStream();
            using var cryptoStream = new CryptoStream(msOutput, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipherText, 0, cipherText.Length);
            cryptoStream.FlushFinalBlock();

            return msOutput.ToArray();
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }
    }
}
