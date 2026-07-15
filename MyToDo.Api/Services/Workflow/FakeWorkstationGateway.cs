using System.Security.Cryptography;
using System.Text;

namespace MyToDo.Api.Services.Workflow
{
    /// <summary>
    /// Fake gateway for local/dev testing.
    /// Returns deterministic outputs so tests and curl demos are stable.
    /// </summary>
    public class FakeWorkstationGateway : IWorkstationGateway
    {
        public Task<IReadOnlyList<string>> GetExperimentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>(["EXP-DEMO-001", "EXP-DEMO-002"]);
        }

        public Task<IReadOnlyDictionary<string, string>> GetExperimentParametersAsync(string experimentCode, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
            {
                ["temperature"] = "25",
                ["durationMinutes"] = "30"
            };
            return Task.FromResult(parameters);
        }

        public Task<StartExperimentResult> StartExperimentAsync(
            string experimentCode,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            // Deterministic GUID to make local tests repeatable for the same input payload.
            var payload = $"{experimentCode}:{string.Join(";", parameters.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"))}";
            var deviceJobId = CreateDeterministicGuid(payload);

            return Task.FromResult(new StartExperimentResult
            {
                DeviceJobId = deviceJobId,
                Accepted = true,
                Message = "Fake gateway accepted experiment."
            });
        }

        public Task<bool> TriggerEventAsync(Guid deviceJobId, string eventCode, CancellationToken cancellationToken)
        {
            // For local usage, treat non-empty event codes as accepted.
            return Task.FromResult(!string.IsNullOrWhiteSpace(eventCode));
        }

        private static Guid CreateDeterministicGuid(string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            var hash = MD5.HashData(bytes);
            return new Guid(hash);
        }
    }
}
