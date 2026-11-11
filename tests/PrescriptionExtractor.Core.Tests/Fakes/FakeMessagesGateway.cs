using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Core.Tests.Fakes;

public sealed class FakeMessagesGateway(Func<GatewayRequest, GatewayResponse> respond) : IMessagesGateway
{
    public List<GatewayRequest> Requests { get; } = [];

    public Task<GatewayResponse> SendAsync(GatewayRequest request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }

    public static FakeMessagesGateway Returning(string json, long input = 1500, long output = 400) =>
        new(_ => new GatewayResponse("end_turn", json, input, output, null));
}
