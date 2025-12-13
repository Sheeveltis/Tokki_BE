using System.ComponentModel;


namespace Tokki.Domain.Enums
{
    public enum UserFavoriteTopicStatus
    {
        [Description("Đang yêu thích")]
        Active = 1,

        [Description("Đã bỏ yêu thích")]
        Removed = 2
    }
}
