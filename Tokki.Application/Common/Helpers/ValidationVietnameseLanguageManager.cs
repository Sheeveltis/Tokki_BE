using FluentValidation.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.Helpers
{
    public class ValidationVietnameseLanguageManager : LanguageManager
    {
        public ValidationVietnameseLanguageManager()
        {

            // --- 1. Cơ bản (Null/Empty) ---
            AddTranslation("en", "NotEmptyValidator", "'{PropertyName}' không được để trống.");
            AddTranslation("en", "NotNullValidator", "'{PropertyName}' không được là null.");
            AddTranslation("en", "NullValidator", "'{PropertyName}' bắt buộc phải là null.");

            // --- 2. Độ dài (Length) ---
            AddTranslation("en", "MaximumLengthValidator", "'{PropertyName}' tối đa {MaxLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("en", "MinimumLengthValidator", "'{PropertyName}' phải có ít nhất {MinLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("en", "LengthValidator", "'{PropertyName}' phải có độ dài từ {MinLength} đến {MaxLength} ký tự.");
            AddTranslation("en", "ExactLengthValidator", "'{PropertyName}' phải có đúng {MaxLength} ký tự.");

            // --- 3. Định dạng (Regex/Email) ---
            AddTranslation("en", "EmailValidator", "'{PropertyName}' không đúng định dạng email.");
            AddTranslation("en", "RegularExpressionValidator", "'{PropertyName}' không đúng định dạng yêu cầu.");

            // --- 4. So sánh số học (Comparison) ---
            AddTranslation("en", "EqualValidator", "'{PropertyName}' phải bằng '{ComparisonValue}'.");
            AddTranslation("en", "NotEqualValidator", "'{PropertyName}' không được bằng '{ComparisonValue}'.");
            AddTranslation("en", "GreaterThanOrEqualValidator", "'{PropertyName}' phải lớn hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("en", "GreaterThanValidator", "'{PropertyName}' phải lớn hơn '{ComparisonValue}'.");
            AddTranslation("en", "LessThanOrEqualValidator", "'{PropertyName}' phải nhỏ hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("en", "LessThanValidator", "'{PropertyName}' phải nhỏ hơn '{ComparisonValue}'.");

            // --- 5. Khoảng giá trị (Between) ---
            AddTranslation("en", "InclusiveBetweenValidator", "'{PropertyName}' phải nằm trong khoảng từ {From} đến {To}.");
            AddTranslation("en", "ExclusiveBetweenValidator", "'{PropertyName}' phải nằm giữa {From} và {To} (không bao gồm 2 đầu).");

            // --- 6. Enum & Khác ---
            AddTranslation("en", "EnumValidator", "'{PropertyName}' có giá trị không hợp lệ.");
            AddTranslation("en", "CreditCardValidator", "'{PropertyName}' không phải là số thẻ tín dụng hợp lệ.");
            AddTranslation("en", "AsyncPredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");
        }
    }
}
