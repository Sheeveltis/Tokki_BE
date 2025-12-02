using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Tokki.Application.Common.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string title, string id)
        {
            if (string.IsNullOrWhiteSpace(title)) return id;

            string slug = title.ToLowerInvariant();

            slug = slug.Replace("đ", "d"); 
            slug = slug.Replace("&", " va "); 

            slug = RemoveDiacritics(slug);

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            return $"{slug}-{id}";
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}