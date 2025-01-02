using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using MonsterCardGame.Server.HttpModel;

namespace MonsterCardGame.Server.HttpLogic
{

    public class HttpRequest
    {
        public Request ReadRequest(StreamReader reader)
        {
            var request = new Request();

            // ----- 1. Read the Request Line -----
            string? line = reader.ReadLine();
            if (line != null)
            {
                Console.WriteLine(line);
                var parts = line.Split(' ');
                if (parts.Length >= 3)
                {
                    request.Method = parts[0];
                    request.Path = parts[1];
                    request.HttpVersion = parts[2];
                }
            }

            // ----- 2. Read the Headers -----
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (string.IsNullOrEmpty(line))
                {
                    break;  // Empty line indicates the end of the headers
                }

                var headerParts = line.Split(new[] { ':' }, 2);
                if (headerParts.Length == 2)
                {
                    var headerName = headerParts[0].Trim();
                    var headerValue = headerParts[1].Trim();
                    request.Headers[headerName] = headerValue;
                }
            }

            // ----- 3. Read the Body -----
            if (request.Headers.TryGetValue("Content-Length", out string? contentLengthValue)
                && int.TryParse(contentLengthValue, out int contentLength)
                && contentLength > 0)
            {
                char[] buffer = new char[contentLength];
                int read = reader.ReadBlock(buffer, 0, contentLength);
                request.Body = new string(buffer, 0, read);
                Console.WriteLine(request.Body);
            }

            return request;
        }
    }
}