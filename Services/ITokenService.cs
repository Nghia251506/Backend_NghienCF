public interface ITokenService
{
    string CreateAccessToken(int userId, string userName, string role);
    (string token, DateTime expires, string hashed) CreateRefreshToken();
}