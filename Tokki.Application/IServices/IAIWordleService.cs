using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.IServices
{
    public interface IAIWordleService
    {
        Task<WordleAiFeedbackDto> EvaluateSentenceAsync(string sentence, string word, string definition);
    }
}
