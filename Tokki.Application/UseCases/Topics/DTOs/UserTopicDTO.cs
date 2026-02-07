using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Topics.DTOs
{
    public class UserTopicDto : TopicDto
    {
        public bool IsLearned { get; set; }
        public int Progress { get; set; }
    }
}
