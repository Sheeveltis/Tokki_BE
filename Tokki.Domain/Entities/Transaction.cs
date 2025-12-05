using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 

        public string Gateway { get; set; } = string.Empty; 
        public DateTimeOffset TransactionDate { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string SubAccount { get; set; } = string.Empty;
        public decimal AmountIn { get; set; } = 0;
        public decimal AmountOut { get; set; } = 0;
        public decimal Accumulated { get; set; } = 0; // Số dư lũy kế
        public string Code { get; set; } = string.Empty; 
        public string TransactionContent { get; set; } = string.Empty; 
        public string ReferenceNumber { get; set; } = string.Empty; 
        public string Body { get; set; } = string.Empty; 
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}