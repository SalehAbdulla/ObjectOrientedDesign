using System;
using System.Security.Cryptography;
using System.Text;

namespace SCAIS.Domain
{
    /// <summary>
    /// Turns a plain text password into the value that is stored in the
    /// Users.PasswordHash column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a separate class.</b> Hashing is a single, well defined responsibility
    /// that must behave identically in three places: when a user is created, when a
    /// user signs in, and when the database is seeded. Putting it in one class means
    /// there is exactly one implementation of the rule.
    /// </para>
    /// <para>
    /// <b>Algorithm.</b> SHA-256 over the UTF-8 bytes of <c>password + "|SCAIS"</c>,
    /// rendered as uppercase hexadecimal. The literal salt is fixed because the
    /// database seed script must be able to compute the same value with
    /// <c>HASHBYTES('SHA2_256', 'password|SCAIS')</c>. A production system should use
    /// a per-user random salt and a deliberately slow algorithm such as PBKDF2 or
    /// bcrypt; that is noted as a limitation in the project report.
    /// </para>
    /// <para>
    /// The plain text password is never stored and never leaves this class.
    /// </para>
    /// </remarks>
    public static class PasswordHasher
    {
        /// <summary>Fixed salt appended to every password before hashing.</summary>
        private const string Salt = "|SCAIS";

        /// <summary>
        /// Returns the uppercase hexadecimal SHA-256 hash of the supplied password.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>A 64 character uppercase hexadecimal string.</returns>
        public static string Hash(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(password + Salt));
                StringBuilder builder = new StringBuilder(hash.Length * 2);

                for (int index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Compares a plain text password with a stored hash.
        /// </summary>
        /// <param name="password">The plain text password supplied by the user.</param>
        /// <param name="storedHash">The hash held in the database.</param>
        /// <returns>True when the password matches the stored hash.</returns>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string candidate = Hash(password);

            // Fixed time comparison avoids leaking information through timing.
            if (candidate.Length != storedHash.Length)
            {
                return false;
            }

            int difference = 0;

            for (int index = 0; index < candidate.Length; index++)
            {
                difference |= char.ToUpperInvariant(candidate[index]) ^ char.ToUpperInvariant(storedHash[index]);
            }

            return difference == 0;
        }
    }
}
