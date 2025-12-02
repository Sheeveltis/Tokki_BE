using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IIdGeneratorService
    {
        // Tạo ID mặc định (21 ký tự)
        string Generate();

        // Tạo ID ngắn tùy chỉnh (VD: 8-10 ký tự cho URL đẹp)
        string Generate(int length);

        // Tạo ID với bảng chữ cái tùy chỉnh (Chỉ số hoặc bỏ ký tự dễ nhầm)
        string GenerateCustom(int length);
    }
}
