namespace Zelenay_MTCG.Server.HttpModel
{

    public class Response
    {
        public string HttpVersion { get; set; } = "HTTP/1.1";
        public int StatusCode { get; set; } = 200;
        public string Reason { get; set; } = "OK";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = "";
    }
}