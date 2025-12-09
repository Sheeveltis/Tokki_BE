using FluentValidation.Resources;

namespace Tokki.Application.Common.Helpers
{
    public class ValidationVietnameseLanguageManager : LanguageManager
    {
        public ValidationVietnameseLanguageManager()
        {
            // --- 1. Cơ bản (Null/Empty) ---
            AddTranslation("vi", "NotEmptyValidator", "'{PropertyName}' không được để trống.");
            AddTranslation("vi", "NotNullValidator", "'{PropertyName}' không được là null.");
            AddTranslation("vi", "NullValidator", "'{PropertyName}' bắt buộc phải là null.");

            // --- 2. Độ dài (Length) ---
            AddTranslation("vi", "MaximumLengthValidator", "'{PropertyName}' tối đa {MaxLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("vi", "MinimumLengthValidator", "'{PropertyName}' phải có ít nhất {MinLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("vi", "LengthValidator", "'{PropertyName}' phải có độ dài từ {MinLength} đến {MaxLength} ký tự.");
            AddTranslation("vi", "ExactLengthValidator", "'{PropertyName}' phải có đúng {MaxLength} ký tự.");

            // --- 3. Định dạng (Regex/Email) ---
            AddTranslation("vi", "EmailValidator", "'{PropertyName}' không đúng định dạng email.");
            AddTranslation("vi", "RegularExpressionValidator", "'{PropertyName}' không đúng định dạng yêu cầu.");

            // --- 4. So sánh số học (Comparison) ---
            AddTranslation("vi", "EqualValidator", "'{PropertyName}' phải bằng '{ComparisonValue}'.");
            AddTranslation("vi", "NotEqualValidator", "'{PropertyName}' không được bằng '{ComparisonValue}'.");
            AddTranslation("vi", "GreaterThanOrEqualValidator", "'{PropertyName}' phải lớn hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("vi", "GreaterThanValidator", "'{PropertyName}' phải lớn hơn '{ComparisonValue}'.");
            AddTranslation("vi", "LessThanOrEqualValidator", "'{PropertyName}' phải nhỏ hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("vi", "LessThanValidator", "'{PropertyName}' phải nhỏ hơn '{ComparisonValue}'.");

            // --- 5. Khoảng giá trị (Between) ---
            AddTranslation("vi", "InclusiveBetweenValidator", "'{PropertyName}' phải nằm trong khoảng từ {From} đến {To}.");
            AddTranslation("vi", "ExclusiveBetweenValidator", "'{PropertyName}' phải nằm giữa {From} và {To} (không bao gồm 2 đầu).");

            // --- 6. Enum & Khác ---
            AddTranslation("vi", "EnumValidator", "'{PropertyName}' có giá trị không hợp lệ.");
            AddTranslation("vi", "CreditCardValidator", "'{PropertyName}' không phải là số thẻ tín dụng hợp lệ.");
            AddTranslation("vi", "AsyncPredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");
        }
    }
}