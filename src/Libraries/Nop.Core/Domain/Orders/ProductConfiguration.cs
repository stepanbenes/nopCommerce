using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Orders
{
    public class ProductConfiguration : BaseEntity
    {
        public int ProductId { get; set; }

        /// <summary>
        /// HiStruct component id (ComponentAggregateId GUID)
        /// </summary>
        public string ComponentId { get; set; }
    }
}
