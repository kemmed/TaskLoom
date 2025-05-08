namespace diplom.Services
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class PasswordService
    {
        /// <summary>
        /// Хеширует пароль с использованием алгоритма SHA256.
        /// </summary>
        /// <param name="password">Пароль для хеширования.</param>
        /// <returns>Хеш пароля.</returns>
        public string HashPassword(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                StringBuilder hashString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashString.Append(b.ToString("x2"));
                }

                return hashString.ToString();
            }
        }

        /// <summary>
        /// Проверяет, является ли пароль допустимым.
        /// Пароль считается допустимым, если он содержит:
        /// - Минимум 8 символов,
        /// - Хотя бы одну заглавную букву,
        /// - Хотя бы одну строчную букву,
        /// - Хотя бы одну цифру,
        /// - Хотя бы один специальный символ.
        /// </summary>
        /// <param name="password">Пароль для проверки.</param>
        /// <returns>Значение true, если пароль допустим, иначе false.</returns>
        public bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSymbol;
        }
    }
}
