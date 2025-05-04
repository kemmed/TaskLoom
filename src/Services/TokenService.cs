namespace diplom.Services
{
    using System;
    public class TokenService
    {
        private readonly Random _rnd = new();

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
