using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.Endpoints;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Server.HttpModel;

namespace Zelenay_MTCG.Server.Endpoints.TradeEndpoint
{
    public class TradeEndpoint : IEndpoint
    {
        private readonly TradeRepository _transactionRepo;
        private readonly UserRepository _userRepo;

        public TradeEndpoint(TradeRepository transactionRepo, UserRepository userRepo)
        {
            _transactionRepo = transactionRepo;
            _userRepo = userRepo;
        }

        public void HandleRequest(Request request, Response response)
        {
            if (request.Method == "POST" && request.Path == "/transactions/packages")
            {
               
                string token = request.Headers.TryGetValue("Authorization", out var authValue)
                    ? authValue
                    : string.Empty;

                string username = GetUsernameFromToken(token);

                if (string.IsNullOrEmpty(username))
                {
                    response.StatusCode = 401;
                    response.Reason = "Unauthorized";
                    response.Body = "Invalid or missing token.";
                    return;
                }

                
                User user = _userRepo.GetUserByUsername(username);
                if (user == null)
                {
                    response.StatusCode = 401;
                    response.Reason = "Unauthorized";
                    response.Body = "User not found.";
                    return;
                }

              
                bool success = _transactionRepo.AcquirePackage(user);
                if (!success)
                {
                    response.StatusCode = 400;  
                    response.Reason = "Bad Request";
                    response.Body = "Not enough money or no packages available.";
                }
                else
                {
                    
                    response.StatusCode = 201;
                    response.Reason = "Created";
                    response.Body = "Package acquired.";
                }
            }
            else
            {
                
                response.StatusCode = 404;
                response.Reason = "Not Found";
                response.Body = "Endpoint not found.";
            }
        }

        private string GetUsernameFromToken(string authHeader)
        {
            if (authHeader.Contains("Bearer"))
            {
                
                string tokenPart = authHeader.Replace("Bearer", "").Trim();
               
                int index = tokenPart.IndexOf("-mtcgToken", System.StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    return tokenPart.Substring(0, index);
                }
            }

            return string.Empty;
        }
    }
}