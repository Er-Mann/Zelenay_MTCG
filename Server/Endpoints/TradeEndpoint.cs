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
                // 1) Identify the user from the token
                //    e.g. "Authorization: Bearer kienboec-mtcgToken"
                string token = request.Headers.TryGetValue("Authorization", out var authValue)
                    ? authValue
                    : string.Empty;

                // Extract username from token, e.g. "kienboec"
                string username = GetUsernameFromToken(token);

                if (string.IsNullOrEmpty(username))
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "Invalid or missing token.";
                    return;
                }

                // 2) Fetch user from DB
                User user = _userRepo.GetUserByUsername(username);
                if (user == null)
                {
                    response.StatusCode = 401;
                    response.ReasonPhrase = "Unauthorized";
                    response.Body = "User not found.";
                    return;
                }

                // 3) Acquire a package
                bool success = _transactionRepo.AcquirePackage(user);
                if (!success)
                {
                    // Either not enough money or no package
                    response.StatusCode = 400;  // or 409 if you prefer
                    response.ReasonPhrase = "Bad Request";
                    response.Body = "Not enough money or no packages available.";
                }
                else
                {
                    // OK => 201 Created
                    response.StatusCode = 201;
                    response.ReasonPhrase = "Created";
                    response.Body = "Package acquired.";
                }
            }
            else
            {
                // No match => 404
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "Endpoint not found.";
            }
        }

        private string GetUsernameFromToken(string authHeader)
        {
            // Minimal example:
            // "Authorization: Bearer kienboec-mtcgToken" => "kienboec"
            // Real logic might parse substring or store in DB.
            if (authHeader.Contains("Bearer"))
            {
                // e.g. "Bearer kienboec-mtcgToken"
                // naive approach:
                string tokenPart = authHeader.Replace("Bearer", "").Trim();
                // tokenPart = "kienboec-mtcgToken"
                // Let’s assume everything up to "-mtcgToken" is username
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