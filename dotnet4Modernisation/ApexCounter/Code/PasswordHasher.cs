using System.Security.Cryptography;
using System.Text;

namespace ApexCounter
{
    /* Legacy rule, on purpose: SHA-1(salt + password), lowercase hex, UTF-8. Not a password store to copy. */
    public static class PasswordHasher
    {
        public static string Hash(string salt, string password)
        {
            SHA1 sha = SHA1.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(salt + password);
            byte[] hash = sha.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
