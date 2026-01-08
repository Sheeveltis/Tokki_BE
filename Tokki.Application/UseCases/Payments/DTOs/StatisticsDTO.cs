namespace Tokki.Application.UseCases.Payments.DTOs
{
    public class DashboardOverviewDto
    {
        public decimal TotalRevenue { get; set; }       
        public int TotalOrders { get; set; }            
        public decimal AverageRevenue { get; set; }     
    }

    public class RevenueChartDto
    {
        public string Month { get; set; } = string.Empty; 
        public decimal Revenue { get; set; }              
        public int TotalOrders { get; set; }             
    }

    public class RevenueByPackageDto
    {
        public string PackageName { get; set; } = string.Empty;
        public int DurationDays { get; set; }    
        public decimal Revenue { get; set; }
        public int SalesCount { get; set; }
        public double Percentage { get; set; }  
    }

    public class TransactionReportDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;    
        public string UserAvatar { get; set; } = string.Empty;    
        public string FullName { get; set; } = string.Empty;      
        public string PackageName { get; set; } = string.Empty;   
        public decimal Amount { get; set; }                      
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;        
        public DateTime PaymentDate { get; set; }                 
    }
}