using FluentValidation;

namespace Tokki.Application.UseCases.Favorite.Commands.AddFavoriteTopic
{
    public class AddFavoriteTopicCommandValidator : AbstractValidator<AddFavoriteTopicCommand>
    {
        public AddFavoriteTopicCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty().WithMessage("ID chủ đề không được để trống.");
        }
    }
}