namespace MyToDo.Api.Entities.Workflow
{
    /// <summary>Represents a physical or virtual workstation device.</summary>
    public class Workstation
    {
        public Guid Id { get; set; }

        /// <summary>Unique short code used in device callbacks (e.g. "WS001").</summary>
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        /// <summary>HTTP/gRPC endpoint of the device gateway (used by IWorkstationGateway).</summary>
        public string? Endpoint { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
    }
}
