namespace Zelenay_MTCG.Server.HttpModel
{

    public class Request
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string HttpVersion { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = "";
}
 }