
using System.ComponentModel;
namespace Tokki.Domain.Enums
{
    public enum PassageMediaType
    {
        [Description("Văn bản")]
        Text = 0,

        [Description("Hình ảnh")]
        Image = 1,

        [Description("Audio")]
        Audio = 2
    }
}
