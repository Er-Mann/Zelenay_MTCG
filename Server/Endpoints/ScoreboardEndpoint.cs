using System.Text.Json;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.HttpModel;

namespace Zelenay_MTCG.Server.Endpoints.ScoreboardEndpoint
{
    public class ScoreboardEndpoint : IEndpoint
    {
        private readonly ScoreboardRepository _scoreboardRepository;

        public ScoreboardEndpoint(ScoreboardRepository scoreboardRepository)
        {
            _scoreboardRepository = scoreboardRepository;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.Reason = "Unauthorized";
                return;
            }

            try
            {
                var scoreboard = _scoreboardRepository.GetScoreboard();
                response.StatusCode = 200;
                response.Reason = "OK";
                response.Body = JsonSerializer.Serialize(scoreboard);
            }
            catch
            {
                response.StatusCode = 500;
                response.Reason = "Internal Server Error";
                response.Body = "Unable to fetch scoreboard.";
            }
        }
    }
}
