using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LPUPlus.Protocol.Messages;
using Microsoft.Extensions.Logging;

namespace LPUPlus.Server.AI;

public sealed class AIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly string? _apiKey;
    private readonly string _modelId = "llama-3.2-90b-vision-preview"; // 11b was decommissioned

    public AIService(HttpClient httpClient, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? throw new InvalidOperationException("GROQ_API_KEY is not configured.");
    }

    public async Task<AIResponseMessage> ProcessRequestAsync(AIRequestMessage request)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new AIResponseMessage
            {
                RequestId = request.RequestId,
                Success = false,
                Error = "GROQ_API_KEY is not configured on the server."
            };
        }

        try
        {
            var contentList = new List<object>
            {
                new { type = "text", text = request.Prompt }
            };



            if (!string.IsNullOrEmpty(request.ImageBase64))
            {
                // Groq API requires raw base64 string without data URI prefix
                var b64 = request.ImageBase64;
                if (b64.StartsWith("data:"))
                {
                    b64 = b64.Substring(b64.IndexOf(',') + 1);
                }
                
                b64 = "data:image/jpeg;base64," + b64; // Wait, let's use the correct openai format
                // Groq OpenAI compatibility expects the standard OpenAI data URI format!
                // Wait, if it fails to decode, it might be due to line breaks or whitespace in base64.
                b64 = b64.Replace("\r", "").Replace("\n", "").Trim();

                contentList.Add(new
                {
                    type = "image_url",
                    image_url = new
                    {
                        url = b64
                    }
                });
            }

            var requestBody = new
            {
                model = "llama-3.2-90b-vision-preview",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = contentList.ToArray()
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            requestMessage.Content = jsonContent;

            var response = await _httpClient.SendAsync(requestMessage);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Groq API Error: {errorText}");
            }
            
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return new AIResponseMessage
            {
                RequestId = request.RequestId,
                Success = true,
                Response = text,
                Provider = "Groq",
                Model = "llama-3.2-90b-vision-preview"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Groq API.");
            return new AIResponseMessage
            {
                RequestId = request.RequestId,
                Success = false,
                Error = "Failed to communicate with AI provider."
            };
        }
    }
}
