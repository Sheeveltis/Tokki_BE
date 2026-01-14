using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.QuestionBanks.DTOs
{
    public class RejectQuestionBankRequest
    {
        public string RejectReason { get; set; } = string.Empty;
    }
}
