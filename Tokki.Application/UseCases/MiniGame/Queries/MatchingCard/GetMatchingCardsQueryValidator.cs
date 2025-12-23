using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Tokki.Application.UseCases.MiniGame.Queries.MatchingCard
{
    public class GetMatchingCardsQueryValidator :   AbstractValidator<GetMatchingCardsQuery>
    {
        public GetMatchingCardsQueryValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty()
                .WithName("TopicId");
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithName("Số lượng từ");
        }
    }
}
