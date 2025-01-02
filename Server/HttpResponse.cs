using System;
using System.IO;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Server.HttpHandler;

namespace Zelenay_MTCG.Server.HttpLogic
{
    public class HttpResponse
    {
        public void SendResponse(StreamWriter writer, Response response)
        {
            var writerAlsoToConsole = new StreamTracer(writer);  // Helper class to write to both client and console
            
            writerAlsoToConsole.WriteLine($"{response.HttpVersion} {response.StatusCode} {response.ReasonPhrase}");

            // Ensure Content-Length header is set
            if (!response.Headers.ContainsKey("Content-Length"))
            {
                response.Headers["Content-Length"] = response.Body.Length.ToString();
            }

            // Write headers
            foreach (var header in response.Headers)
            {
                writerAlsoToConsole.WriteLine($"{header.Key}: {header.Value}");
            }

            // Empty line to indicate end of headers
            writerAlsoToConsole.WriteLine();

            // Write the body
            if (!string.IsNullOrEmpty(response.Body))
            {
                writerAlsoToConsole.WriteLine(response.Body);
            }

            Console.WriteLine("========================================");
        }
    }
}