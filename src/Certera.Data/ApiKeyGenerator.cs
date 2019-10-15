using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Certera.Data
{
    public static class ApiKeyGenerator
    {
        public static string CreateApiKey()
        {
            var bytes = new byte[256 / 8];
            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return ToBase62String(bytes);
        }

        private static string ToBase62String(byte[] toConvert)
        {
            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            BigInteger dividend = new BigInteger(toConvert);
            var builder = new StringBuilder();
            while (dividend != 0)
            {
                dividend = BigInteger.DivRem(dividend, alphabet.Length, out BigInteger remainder);
                builder.Insert(0, alphabet[Math.Abs(((int)remainder))]);
            }
            return builder.ToString();
        }
    }
}
