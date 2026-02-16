using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace LEOMVP.Azure_Function
{
    /// <summary>
    /// HTTP-triggered Azure Function (Isolated Worker).
    /// Clean, minimal implementation using the Functions Worker Http types.
    /// </summary>
    public class AddAdoFunction
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger<AddAdoFunction> _logger;

        public AddAdoFunction(ILogger<AddAdoFunction> logger)
        {
            _logger = logger;
        }

        [Function("AddAdo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var raw = await new StreamReader(req.Body).ReadToEndAsync();

            EmailPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<EmailPayload>(
                    raw,
                    options: _jsonOptions
                );
            }
            catch
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid JSON");
                return bad;
            }

            if (payload is null ||
                string.IsNullOrWhiteSpace(payload.Sender) ||
                string.IsNullOrWhiteSpace(payload.Receiver) ||
                string.IsNullOrWhiteSpace(payload.Subject))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Missing required fields: sender, receiver, subject");
                return bad;
            }

            _logger.LogInformation("Sender={Sender} Receiver={Receiver} Subject={Subject}",
                payload.Sender, payload.Receiver, payload.Subject);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new
            {
                received = true,
                payload.Sender,
                payload.Receiver,
                payload.Subject,
                bodyLength = payload.Body?.Length ?? 0
            });

            return ok;
        }


        private class EmailPayload
        {
            public string? Sender { get; set; }
            public string? Receiver { get; set; }
            public string? Subject { get; set; }
            public string? Body { get; set; }
        }
    }
}
