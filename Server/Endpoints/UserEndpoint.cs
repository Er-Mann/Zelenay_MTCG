using System.Text.Json;
using Zelenay_MTCG.Server.HttpModel;
using Zelenay_MTCG.Models.Usermodel;
using Zelenay_MTCG.Repository_DB;

namespace Zelenay_MTCG.Server.Endpoints.Userendpoint
{
    public class UserEndpoint : IEndpoint
    {
        private readonly UserRepository _userRepository;

        public UserEndpoint(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public void HandleRequest(Request request, Response response)
        {
            string jsonText = request.Body;
            var user = JsonSerializer.Deserialize<User>(jsonText);
            if (user == null || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                response.StatusCode = 400;
                response.ReasonPhrase = "Bad Request";
                response.Body = "Invalid user data. Username and password are required.";
            }
            else if (request.Method == "POST" && request.Path == "/users")
            {
                RegisterUser(request, response, user);
            }
            else if (request.Method == "POST" && request.Path == "/sessions")
            {
                LoginUser(request, response, user);
            }
            else if (request.Method == "GET" && request.Path.StartsWith("/users/"))
            {
                ShowUserProfile(request, response);                                 //hier wird nicht user dazugegeben sondern mit einer extra methode user aus db geholt
            }
            else if (request.Method == "PUT" && request.Path.StartsWith("/users/"))
            {
                EditUserProfile(request, response);
            }
            else
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
            }
        }
        private void RegisterUser(Request request, Response response, User user)
        {
            var existingUser = _userRepository.GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                response.StatusCode = 408;
                response.ReasonPhrase = "Already exists";
                response.Body = "HTTP " + response.StatusCode + " - User already exists\n";
            }
            else
            {
                _userRepository.AddUser(user);
                response.StatusCode = 201;
                response.ReasonPhrase = "Created";
                response.Body = "HTTP " + response.StatusCode + "\n";
            }
        }

        private void LoginUser(Request request, Response response, User user)
        {
            // Fetch user from DB
            var dbUser = _userRepository.GetUserByUsername(user.Username);

            // If user doesn't exist OR password mismatch => Unauthorized
            if (dbUser == null || dbUser.Password != user.Password)
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "HTTP " + response.StatusCode + " - Login failed\n";
            }
            else
            {
                user.AuthToken = user.Username + "-mtcgToken";
                response.StatusCode = 200;
                response.ReasonPhrase = "OK";
                response.Body = "HTTP " + response.StatusCode + " " + user.AuthToken + "\n";
            }
        }

        private void ShowUserProfile(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "Missing authorization header.";
                return;
            }

            string usernameFromToken = ExtractUsernameFromToken(authHeader);
            string usernameFromPath = request.Path.Substring("/users/".Length);

            if (usernameFromToken != usernameFromPath)
            {
                response.StatusCode = 403; // Forbidden
                response.ReasonPhrase = "Forbidden";
                response.Body = "You are not authorized to view this user's profile.";
                return;
            }

            var user = _userRepository.GetUserProfile(usernameFromPath);
            if (user == null)
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "User not found.";
                return;
            }

            user.Password = null; // Mask sensitive fields
            user.AuthToken = null;

            response.StatusCode = 200;
            response.ReasonPhrase = "OK";
            response.Body = JsonSerializer.Serialize(user);
        }

        private void EditUserProfile(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "Missing authorization header.";
                return;
            }

            string usernameFromToken = ExtractUsernameFromToken(authHeader);
            string usernameFromPath = request.Path.Substring("/users/".Length);

            if (usernameFromToken != usernameFromPath)
            {
                response.StatusCode = 403; // Forbidden
                response.ReasonPhrase = "Forbidden";
                response.Body = "You can only edit your own profile.";
                return;
            }

            var user = _userRepository.GetUserProfile(usernameFromPath);
            if (user == null)
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "User not found.";
                return;
            }

            var updatedData = JsonSerializer.Deserialize<UserUpdateRequest>(request.Body);
            if (updatedData == null)
            {
                response.StatusCode = 400;
                response.ReasonPhrase = "Bad Request";
                response.Body = "Invalid JSON body.";
                return;
            }

            _userRepository.UpdateUserProfile(user.UserId, updatedData.Name, updatedData.Bio, updatedData.Image);

            response.StatusCode = 200;
            response.ReasonPhrase = "OK";
            response.Body = "User profile updated successfully.";
        }

        private string ExtractUsernameFromToken(string authHeader)
        {
            if (authHeader.Contains("Bearer"))
            {
                string token = authHeader.Replace("Bearer", "").Trim();
                return token.Split("-")[0]; // e.g., "kienboec" from "kienboec-mtcgToken"
            }
            return string.Empty;
        }

        public void HandleGetUser(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "Missing authorization header.";
                return;
            }

            string usernameFromToken = ExtractUsernameFromToken(authHeader);
            string usernameFromPath = request.Path.Substring("/users/".Length);

            if (usernameFromToken != usernameFromPath)
            {
                response.StatusCode = 403; // Forbidden
                response.ReasonPhrase = "Forbidden";
                response.Body = "You are not authorized to view this user's profile.";
                return;
            }

            var user = _userRepository.GetUserProfile(usernameFromPath);
            if (user == null)
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "User not found.";
                return;
            }

            user.Password = null; 
            user.AuthToken = null;

            response.StatusCode = 200;
            response.ReasonPhrase = "OK";
            response.Body = JsonSerializer.Serialize(user);
        }
        public void HandleUpdateUser(Request request, Response response)
        {
            if (!request.Headers.TryGetValue("Authorization", out string authHeader))
            {
                response.StatusCode = 401;
                response.ReasonPhrase = "Unauthorized";
                response.Body = "Missing authorization header.";
                return;
            }

            string usernameFromToken = ExtractUsernameFromToken(authHeader);
            string usernameFromPath = request.Path.Substring("/users/".Length);

            if (usernameFromToken != usernameFromPath)
            {
                response.StatusCode = 403; 
                response.ReasonPhrase = "Forbidden";
                response.Body = "You can only edit your own profile.";
                return;
            }

            var user = _userRepository.GetUserProfile(usernameFromPath);
            if (user == null)
            {
                response.StatusCode = 404;
                response.ReasonPhrase = "Not Found";
                response.Body = "User not found.";
                return;
            }

            var updatedData = JsonSerializer.Deserialize<UserUpdateRequest>(request.Body);
            if (updatedData == null)
            {
                response.StatusCode = 400;
                response.ReasonPhrase = "Bad Request";
                response.Body = "Invalid JSON body.";
                return;
            }

            _userRepository.UpdateUserProfile(user.UserId, updatedData.Name, updatedData.Bio, updatedData.Image);

            response.StatusCode = 200;
            response.ReasonPhrase = "OK";
            response.Body = "User profile updated successfully.";
        }

        public class UserUpdateRequest
        {
            public string Name { get; set; }
            public string Bio { get; set; }
            public string Image { get; set; }
        }
    }
}
