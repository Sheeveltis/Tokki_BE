using System.Text.Json.Serialization; 
namespace Tokki.Domain.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportStatus
    {
        Pending = 0,
        Processing = 1,
        Fixed = 2,
        Rejected = 3
    }
}