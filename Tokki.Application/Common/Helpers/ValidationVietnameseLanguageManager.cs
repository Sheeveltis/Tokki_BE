using FluentValidation.Resources;

namespace Tokki.Application.Common.Helpers
{
    public class ValidationVietnameseLanguageManager : LanguageManager
    {
        public ValidationVietnameseLanguageManager()
        {
            // Thêm cả "en" VÀ "vi" để đảm bảo
            Culture = new System.Globalization.CultureInfo("vi");

            // --- 1. Cơ bản (Null/Empty) ---
            AddTranslation("vi", "NotEmptyValidator", "'{PropertyName}' không được để trống.");
            AddTranslation("vi", "NotNullValidator", "'{PropertyName}' không được là null.");
            AddTranslation("vi", "NullValidator", "'{PropertyName}' bắt buộc phải là null.");

            // Thêm bản tiếng Anh override (quan trọng!)
            AddTranslation("en", "NotEmptyValidator", "'{PropertyName}' không được để trống.");
            AddTranslation("en", "NotNullValidator", "'{PropertyName}' không được là null.");
            AddTranslation("en", "NullValidator", "'{PropertyName}' bắt buộc phải là null.");

            // --- 2. Độ dài (Length) ---
            AddTranslation("vi", "MaximumLengthValidator", "'{PropertyName}' tối đa {MaxLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("vi", "MinimumLengthValidator", "'{PropertyName}' phải có ít nhất {MinLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("vi", "LengthValidator", "'{PropertyName}' phải có độ dài từ {MinLength} đến {MaxLength} ký tự.");
            AddTranslation("vi", "ExactLengthValidator", "'{PropertyName}' phải có đúng {MaxLength} ký tự.");

            AddTranslation("en", "MaximumLengthValidator", "'{PropertyName}' tối đa {MaxLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("en", "MinimumLengthValidator", "'{PropertyName}' phải có ít nhất {MinLength} ký tự. Bạn đã nhập {TotalLength} ký tự.");
            AddTranslation("en", "LengthValidator", "'{PropertyName}' phải có độ dài từ {MinLength} đến {MaxLength} ký tự.");
            AddTranslation("en", "ExactLengthValidator", "'{PropertyName}' phải có đúng {MaxLength} ký tự.");

            // --- 3. Định dạng (Regex/Email) ---
            AddTranslation("vi", "EmailValidator", "'{PropertyName}' không đúng định dạng email.");
            AddTranslation("vi", "RegularExpressionValidator", "'{PropertyName}' không đúng định dạng yêu cầu.");

            AddTranslation("en", "EmailValidator", "'{PropertyName}' không đúng định dạng email.");
            AddTranslation("en", "RegularExpressionValidator", "'{PropertyName}' không đúng định dạng yêu cầu.");

            // --- 4. So sánh số học (Comparison) ---
            AddTranslation("vi", "EqualValidator", "'{PropertyName}' phải bằng '{ComparisonValue}'.");
            AddTranslation("vi", "NotEqualValidator", "'{PropertyName}' không được bằng '{ComparisonValue}'.");
            AddTranslation("vi", "GreaterThanOrEqualValidator", "'{PropertyName}' phải lớn hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("vi", "GreaterThanValidator", "'{PropertyName}' phải lớn hơn '{ComparisonValue}'.");
            AddTranslation("vi", "LessThanOrEqualValidator", "'{PropertyName}' phải nhỏ hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("vi", "LessThanValidator", "'{PropertyName}' phải nhỏ hơn '{ComparisonValue}'.");

            AddTranslation("en", "EqualValidator", "'{PropertyName}' phải bằng '{ComparisonValue}'.");
            AddTranslation("en", "NotEqualValidator", "'{PropertyName}' không được bằng '{ComparisonValue}'.");
            AddTranslation("en", "GreaterThanOrEqualValidator", "'{PropertyName}' phải lớn hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("en", "GreaterThanValidator", "'{PropertyName}' phải lớn hơn '{ComparisonValue}'.");
            AddTranslation("en", "LessThanOrEqualValidator", "'{PropertyName}' phải nhỏ hơn hoặc bằng '{ComparisonValue}'.");
            AddTranslation("en", "LessThanValidator", "'{PropertyName}' phải nhỏ hơn '{ComparisonValue}'.");

            // --- 5. Khoảng giá trị (Between) ---
            AddTranslation("vi", "InclusiveBetweenValidator", "'{PropertyName}' phải nằm trong khoảng từ {From} đến {To}.");
            AddTranslation("vi", "ExclusiveBetweenValidator", "'{PropertyName}' phải nằm giữa {From} và {To} (không bao gồm 2 đầu).");

            AddTranslation("en", "InclusiveBetweenValidator", "'{PropertyName}' phải nằm trong khoảng từ {From} đến {To}.");
            AddTranslation("en", "ExclusiveBetweenValidator", "'{PropertyName}' phải nằm giữa {From} và {To} (không bao gồm 2 đầu).");

            // --- 6. Enum & Khác ---
            AddTranslation("vi", "EnumValidator", "'{PropertyName}' có giá trị không hợp lệ.");
            AddTranslation("vi", "CreditCardValidator", "'{PropertyName}' không phải là số thẻ tín dụng hợp lệ.");
            AddTranslation("vi", "AsyncPredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");
            AddTranslation("vi", "PredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");

            AddTranslation("en", "EnumValidator", "'{PropertyName}' có giá trị không hợp lệ.");
            AddTranslation("en", "CreditCardValidator", "'{PropertyName}' không phải là số thẻ tín dụng hợp lệ.");
            AddTranslation("en", "AsyncPredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");
            AddTranslation("en", "PredicateValidator", "'{PropertyName}' không thỏa mãn điều kiện kiểm tra.");
        }
    }
}