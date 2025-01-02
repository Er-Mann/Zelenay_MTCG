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
        // We expect: POST /packages, admin auth token
        if (request.Method == "POST" && request.Path == "/packages")
        {
            // Check if the user is admin
            // e.g., parse token from request.Headers["Authorization"]
            // Here we just assume it's valid for demonstration
            bool isAdmin = IsAdmin(request.Headers);

            if (!isAdmin)
            {
                response.StatusCode = 403;
                response.ReasonPhrase = "Forbidden";
                response.Body = "Only admin can create packages.";
                return;
            }

            // Deserialize the card array from the request body
            var cards = JsonSerializer.Deserialize<List<Card>>(request.Body);
            if (cards == null || cards.Count == 0)
            {
                response.StatusCode = 400;
                response.ReasonPhrase = "Bad Request";
                response.Body = "No cards provided.";
                return;
            }

            // Let the repo create the package
            _packageRepository.CreatePackage(cards);

            response.StatusCode = 201;
            response.ReasonPhrase = "Created";
            response.Body = "Package created successfully.";
        }
        else
        {
            // If the endpoint or method doesn't match, return 404
            response.StatusCode = 404;
            response.ReasonPhrase = "Not Found";
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
