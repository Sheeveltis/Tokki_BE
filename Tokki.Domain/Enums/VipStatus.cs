using System.ComponentModel;
namespace Tokki.Domain.Enums
{
    public enum VipStatus
    {
        [Description("Còn hạn VIP")]
        Active=1,
        [Description("Không có VIP")]
        NoVip=2
    }
}
