namespace diplom.Services
{
    using System;
    public class TokenService
    {
        private readonly Random _rnd = new();

        /// <summary>
        /// Генерирует случайный токен длиной 16 символов.
        /// Токен состоит из букв английского алфавита (от 'a' до 'z').
        /// </summary>
        /// <returns>Сгенерированный токен.</returns>
        public string GenerateToken()
        {
            string token = "";
            for (int i = 0; i < 16; i++)
            {
                token += (char)('a' + _rnd.Next(0, 28));
            }
            return token;
        }
    }
}
