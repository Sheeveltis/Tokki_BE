using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Enums
{
    public enum QualityVocab
    {
        //Này là khi tự luận nó sai hết không đúng gì
        //Nếu là trắc nghiệm sai thì là đây
        [Description("Chưa nhớ lại được")]
        Again = 0,
        //Này là khi tự luận nó đúng 1 chút 
        [Description("Nhớ được một phần")]
        Good = 1,
        //Này là khi tự luận nó đúng hoàn toàn 100%
        //Nếu là trắc nghiệm đúng thì là đây
        [Description("Nhớ lại dễ dàng")]
        Easy = 2
    }
}
