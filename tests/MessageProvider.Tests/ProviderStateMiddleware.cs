using System.Net;
using System.Text;
using System.Text.Json;
using PactNet;

public class ProviderStateMiddleware
{
    private readonly Dictionary<string, Func<Task>> _providerStates;
    private readonly RequestDelegate _next;

    private static object message;

    public static object getMessage() => message;


    public ProviderStateMiddleware(RequestDelegate next)
    {
        _next = next;
        _providerStates = new Dictionary<string, Func<Task>>()
        {
            ["user exists"] = EnsureUserExists,
            ["user verified"] = EnsureUserIsVerified,
        };
    }

    private Task EnsureUserExists()
    {
        FileLog.Log("State handler - user exists");
        message = new
        {
            id = 1,
            name = "testuser",
            email = "testuser@mail.com"
        };
        return Task.CompletedTask;
    }

    private Task EnsureUserIsVerified()
    {
        FileLog.Log("State handler - user verified");
        message = new
        {
            id = 1,
            name = "testuser",
            email = "testuser@mail.com",
            verified = true
        };
        return Task.CompletedTask;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path
            .Value?.StartsWith("/provider-states") ?? false)
        {
            await _next.Invoke(context);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.OK;

        if (context.Request.Method == HttpMethod.Post.ToString())
        {
            string jsonRequestBody;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                jsonRequestBody = await reader.ReadToEndAsync();
            }

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var providerState = JsonSerializer.Deserialize<ProviderState>(jsonRequestBody, options);

            if (string.IsNullOrEmpty(providerState?.State)
                || !_providerStates.TryGetValue(providerState.State, out Func<Task>? callback))
            {
                return;
            }

            await callback.Invoke();
        }
    }
}
