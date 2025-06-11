using System.Text.Json;
using PactNet.Verifier;

namespace MessageProvider.Tests
{
    [TestClass]
    public class MessageProviderPactTests
    {
        private readonly string ProviderUrl = "http://localhost:5000";
        public MessageProviderPactTests()
        {
            FileLog.DeleteLog();

            var app = WebApplication.CreateBuilder().Build();
            app.UseMiddleware<ProviderStateMiddleware>();
            app.Urls.Add(ProviderUrl);
            app.Start();
        }

        [TestMethod]
        public void VerifyMessagePacts()
        {
            var verifier = new PactVerifier("MessageProvider");

            verifier
                .WithHttpEndpoint(new Uri(ProviderUrl))
                .WithMessages(scenarios =>
                {
                    scenarios.Add(
                        "user was created event", builder =>
                        {
                            FileLog.Log("Description handler: user was created event");

                            builder.WithMetadata(
                                ProviderStateMiddleware.CreateUserWasCreatedMetadata()
                            );
                            builder.WithContent(() =>
                            {
                                FileLog.Log("WithContent: user was created event");
                                return ProviderStateMiddleware.CreateUserWasCreatedMessage();
                            }
                            );
                        });
                }, new JsonSerializerOptions())
                .WithFileSource(new FileInfo("../../../../../MessageConsumer-MessageProvider.json"))
                .WithProviderStateUrl(new Uri("http://localhost:5000/provider-states"))
                .Verify();
        }
    }
}
