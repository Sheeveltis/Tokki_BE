using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.Common.ExcelCore
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelColumnAttribute : Attribute
    {
        public string ColumnName { get; }
        public int Order { get; set; }

        public ExcelColumnAttribute(string columnName, int order = 99)
        {
            ColumnName = columnName;
            Order = order;
        }
    }
}
