using Zelenay_MTCG.Server.HttpModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Zelenay_MTCG.Repository_DB;
using Zelenay_MTCG.Server.Endpoints;
using Zelenay_MTCG.Models.Package;
using Zelenay_MTCG.Models.Cards;

namespace Zelenay_MTCG.Server.Endpoints.Packageendpoint;

public class PackageEndpoint : IEndpoint
{
    private readonly PackageRepository _packageRepository;

    public PackageEndpoint(PackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public void HandleRequest(Request request, Response response)
    {
        // expect: POST /packages, admin auth token
        if (request.Method == "POST" && request.Path == "/packages")
        {
           
            bool isAdmin = IsAdmin(request.Headers);

            if (!isAdmin)
            {
                response.StatusCode = 403;
                response.Reason = "Forbidden";
                response.Body = "Only admin can create packages.";
                return;
            }

            // Deserialize the card array from the request body
            var cards = JsonSerializer.Deserialize<List<Card>>(request.Body);
            if (cards == null || cards.Count == 0)
            {
                response.StatusCode = 400;
                response.Reason = "Bad Request";
                response.Body = "No cards provided.";
                return; 
            }
            if (_packageRepository.CheckUniqueCardIds(cards)) {
                response.StatusCode = 407;
                response.Reason = "Bad Request";
                response.Body = "Cardid already exists.";
                return;
            }
            _packageRepository.CreatePackage(cards);

            response.StatusCode = 201;
            response.Reason = "Created";
            response.Body = "Package created successfully.";
        }
        else
        {
            
            response.StatusCode = 404;
            response.Reason = "Not Found";
            response.Body = "Endpoint not found.";
        }
    }

    private bool IsAdmin(IDictionary<string, string> headers)
    {
        // Minimal demo check:
        // e.g., "Authorization: Bearer admin-mtcgToken"
        if (headers.TryGetValue("Authorization", out string token))
        {
            return token.Contains("admin-mtcgToken");
        }
        return false;
    }
}
