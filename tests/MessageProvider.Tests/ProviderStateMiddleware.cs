using System.Net;
using System.Text;
using System.Text.Json;
using PactNet;

public class ProviderStateMiddleware
{
    private readonly Dictionary<string, Func<Task>> _providerStates;
    private readonly RequestDelegate _next;

    private static bool _isSocialAccountRegistered = false;

    public static object CreateUserWasCreatedMessage()
    {
        FileLog.Log("Creating message for user creation event");

        if (_isSocialAccountRegistered)
        {
            return new
            {
                id = 1,
                name = "testuser",
                email = "testuser@mail.com",
                social = true
            };
        }

        return new
        {
            id = 1,
            name = "testuser",
            email = "testuser@mail.com",
            password = "testpassword",
        };
    }

    public ProviderStateMiddleware(RequestDelegate next)
    {
        _next = next;
        _providerStates = new Dictionary<string, Func<Task>>()
        {
            ["user registers with email and password"] = EnsureUserRegisters,
            ["user registers with social account"] = EnsureUserRegistersWithSocialAccount,
        };
    }

    private Task EnsureUserRegisters()
    {
        FileLog.Log("State handler - user registers with email and password");
        _isSocialAccountRegistered = false;
        return Task.CompletedTask;
    }

    private Task EnsureUserRegistersWithSocialAccount()
    {
        FileLog.Log("State handler - user registers with social account");
        _isSocialAccountRegistered = true;
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

        // context.Response.StatusCode = (int)HttpStatusCode.OK;

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
                throw new ArgumentException(
                    $"Unknown provider state: {providerState?.State ?? "null"}");
            }

            await callback.Invoke();
        }
    }
}
