using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Accounts.DTOs;

namespace Tokki.Application.UseCases.Accounts.Commands.FacebookLogin
{
    public class FacebookLoginCommand : IRequest<OperationResult<FacebookLoginResponse>>
    {
        public bool IsComfirmToMergeAcc { get; set; } = false;
        public string AccessToken { get; set; } = string.Empty;
    }
}
