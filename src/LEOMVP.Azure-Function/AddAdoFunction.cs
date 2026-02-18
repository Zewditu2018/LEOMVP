using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace LEOMVP.Azure_Function
{
    public class AddAdoFunction
    {
        private readonly ILogger<AddAdoFunction> _logger;

        public AddAdoFunction(ILogger<AddAdoFunction> logger)
        {
            _logger = logger;
        }

        [Function("AddAdo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var requestId = Guid.NewGuid().ToString();

            
            if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var health = req.CreateResponse(HttpStatusCode.OK);
                await health.WriteAsJsonAsync(new
                {
                    status = "Running",
                    requestId,
                    message = "POST JSON to this endpoint"
                });
                return health;
            }

            string rawBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return await BadRequest(req, requestId, "Request body is empty");
            }

            JsonElement json;

            try
            {
                json = JsonSerializer.Deserialize<JsonElement>(rawBody);
            }
            catch (JsonException)
            {
                return await BadRequest(req, requestId, "Invalid JSON format");
            }

           
            var keys = new List<string>();
            if (json.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in json.EnumerateObject())
                    keys.Add(prop.Name);
            }

            _logger.LogInformation("Request {RequestId} received with keys: {Keys}",
                requestId, string.Join(",", keys));

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new AgentResponse
            {
                Success = true,
                RequestId = requestId,
                DetectedFields = keys,
                Payload = json
            });

            return ok;
        }

        

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string requestId, string message)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new AgentResponse
            {
                Success = false,
                RequestId = requestId,
                Error = message
            });
            return bad;
        }

       

        private class AgentResponse
        {
            public bool Success { get; set; }
            public string? RequestId { get; set; }
            public string? Error { get; set; }
            public List<string>? DetectedFields { get; set; }
            public JsonElement? Payload { get; set; }
        }
    }
}