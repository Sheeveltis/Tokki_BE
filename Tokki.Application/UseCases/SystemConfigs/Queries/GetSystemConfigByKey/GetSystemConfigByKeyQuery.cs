using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.SystemConfigs.DTOs;

namespace Tokki.Application.UseCases.SystemConfigs.Queries.GetSystemConfigByKey
{
    public class GetSystemConfigByKeyQuery : IRequest<OperationResult<SystemConfigDto>>
    {
        public string Key { get; set; } = string.Empty;

        public GetSystemConfigByKeyQuery(string key)
        {
            Key = key;
        }
    }
}
