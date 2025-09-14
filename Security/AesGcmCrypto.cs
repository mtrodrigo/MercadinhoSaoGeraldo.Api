using System;
using System.Security.Cryptography;
using System.Text;

namespace MercadinhoSaoGeraldo.Api.Security
{
    public static class AesGcmCrypto
    {
        public static string Encrypt(string plaintext, byte[] key)
        {
            using var aes = new AesGcm(key, 16);

            var nonce = RandomNumberGenerator.GetBytes(12);
            var pt = Encoding.UTF8.GetBytes(plaintext);
            var ct = new byte[pt.Length];
            var tag = new byte[16];

            aes.Encrypt(nonce, pt, ct, tag);

            var packed = new byte[nonce.Length + tag.Length + ct.Length];
            Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
            Buffer.BlockCopy(ct, 0, packed, nonce.Length + tag.Length, ct.Length);

            return Convert.ToBase64String(packed);
        }

        public static string Decrypt(string base64, byte[] key)
        {
            var packed = Convert.FromBase64String(base64);

            var nonce = new byte[12];
            var tag = new byte[16];
            var ct = new byte[packed.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(packed, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(packed, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(packed, nonce.Length + tag.Length, ct, 0, ct.Length);

            using var aes = new AesGcm(key, tag.Length);

            var pt = new byte[ct.Length];
            aes.Decrypt(nonce, ct, tag, pt);

            return Encoding.UTF8.GetString(pt);
        }
    }
}
