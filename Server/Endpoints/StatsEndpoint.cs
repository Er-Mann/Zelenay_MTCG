using System.Text.Json;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.HttpModel;

namespace Zelenay_MTCG.Server.Endpoints.StatsEndpoint
{
    public class StatsEndpoint : IEndpoint
    {
        private readonly StatsRepository _statsRepository;

        public StatsEndpoint(StatsRepository statsRepository)
        {
            _statsRepository = statsRepository;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            string username = ExtractUsernameFromToken(authHeader);
            if (string.IsNullOrEmpty(username))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            try
            {
                var userStats = _statsRepository.GetUserStats(username);
                response.StatusCode = 200;
                response.Reason = "OK";
                response.Body = JsonSerializer.Serialize(userStats);
            }
            catch
            {
                response.StatusCode = 404;
                response.Reason = "Not Found";
                response.Body = "User stats not found.";
            }
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            if (authHeader.Contains("Bearer"))
            {
                string token = authHeader.Replace("Bearer", "").Trim();
                return token.Split("-")[0];
            }
            return string.Empty;
        }
    }
}
