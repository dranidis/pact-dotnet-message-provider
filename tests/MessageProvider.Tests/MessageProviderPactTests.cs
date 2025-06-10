using System.Text.Json;
using PactNet;
using PactNet.Verifier;

namespace MessageProvider.Tests
{
    [TestClass]
    public class MessageProviderPactTests
    {
        private readonly string ProviderUrl = "http://localhost:5000";
        public MessageProviderPactTests()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

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
                        "a user created event", builder =>
                        {
                            FileLog.Log("Description handler");

                            builder.WithContent(() =>
                            {
                                FileLog.Log("WithContent");
                                // return new
                                // {
                                //     id = 1,
                                //     name = "testuser",
                                //     email = "testuser@mail.com"
                                // };
                                return ProviderStateMiddleware.getMessage();
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
