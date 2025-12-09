using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NanoidDotNet; 
using Tokki.Application.IServices;
namespace Tokki.Infrastructure.Services
{
    public class IdGeneratorService : IIdGeneratorService
    {
        private const string _customAlphabet = "23456789abcdefghijkmnpqrstuvwxyz";

        // Mặc định NanoID sinh 21 ký tự
        public string Generate()
        {
          
            return Nanoid.Generate();
        }

        public string Generate(int length)
        {
            return Nanoid.Generate(size: length);
        }

        // Dùng bảng chữ cái riêng của mình để ID nhìn sạch đẹp hơn
        public string GenerateCustom(int length)
        {
            return Nanoid.Generate(_customAlphabet, length);
        }
    }
}
