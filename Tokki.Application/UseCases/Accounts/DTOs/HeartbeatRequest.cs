using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class HeartbeatRequest
    {
        public string UserId { get; set; }
        public double DurationInSeconds { get; set; }
    }
}
