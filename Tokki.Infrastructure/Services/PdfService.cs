using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Tokki.Infrastructure.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConverter _converter;

        public PdfService(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] GeneratePdfFromHtml(string htmlContent, string? title, string? headerHtmlPath)
        {
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 10, Right = 10 },
                    DocumentTitle = title ?? "Tokki Export"
                },
                Objects = {
                    new ObjectSettings() {
                        PagesCount = true,
                        HtmlContent = htmlContent,
                        WebSettings = { 
                            DefaultEncoding = "utf-8", 
                            LoadImages = true, 
                            EnableIntelligentShrinking = false 
                        },
                        HeaderSettings = { 
                            HtmlUrl = headerHtmlPath,
                            Line = false,
                            Spacing = 0
                        },
                        FooterSettings = { 
                            FontName = "Arial", 
                            FontSize = 10, 
                            Center = "[page]", 
                            Line = false,
                            Spacing = 5
                        }
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
