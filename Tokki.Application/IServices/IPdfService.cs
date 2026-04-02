using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IPdfService
    {
        byte[] GeneratePdfFromHtml(string htmlContent, string? title, string? headerHtmlPath);
    }
}
