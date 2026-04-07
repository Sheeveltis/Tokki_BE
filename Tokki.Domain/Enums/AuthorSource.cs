using System.ComponentModel;
 
namespace Tokki.Domain.Enums
 {
    public enum AuthorSource
    {
        [Description("Tất cả")]
        All = 0,
 
        [Description("Cộng đồng")]
        Community = 1,
 
        [Description("Nội bộ")]
        Internal = 2
    }
 }
