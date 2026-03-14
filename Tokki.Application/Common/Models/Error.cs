using System.Text.Json.Serialization;

namespace Tokki.Application.Common.Models
{
    public sealed record Error(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("description")] string Description)
    {
        public static readonly Error None = new(string.Empty, string.Empty);

        public static readonly Error NullValue = new("Error.NullValue", "Giá trị không được phép null.");
    }
}