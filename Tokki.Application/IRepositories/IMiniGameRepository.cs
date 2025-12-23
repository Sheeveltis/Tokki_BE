using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IMiniGameRepository
    {
        Task<List<Vocabulary>> GetRandomVocabulariesByTopicAsync(string topicId, int quantity, CancellationToken cancellationToken);
    }
}
