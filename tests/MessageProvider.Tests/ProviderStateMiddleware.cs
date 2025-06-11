using System.Text;
using System.Text.Json;
using PactNet;

public class ProviderStateMiddleware
{
    private readonly Dictionary<string, Func<Task>> _providerStates;
    private readonly RequestDelegate _next;

    private static bool _isSocialAccountRegistered = false;

    public static Dictionary<string, string> CreateUserWasCreatedMetadata()
    {
        FileLog.Log("Creating metadata for user creation event");

        // It is not possible to have conditional logic in metadata creation
        // based on the state of the provider, so we return a static metadata
        // regardless of the state. 
        // WithMetadata in PactNet does not receive a callback function to get the metadata
        // dynamically after the state is set.

        // if (_isSocialAccountRegistered)
        // {
        //     return new Dictionary<string, string>
        //     {
        //         { "queue", "user-created-social" },
        //     };
        // }

        return new Dictionary<string, string>
        {
            { "queue", "user-created" },
        };
    }
    public static object CreateUserWasCreatedMessage()
    {
        if (_isSocialAccountRegistered)
        {
            FileLog.Log("Creating message for user creation event, social account registered");
            return new
            {
                id = 1,
                name = "testuser",
                email = "testuser@mail.com",
                social = true
            };
        }

        FileLog.Log("Creating message for user creation event, email and password registered");
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
