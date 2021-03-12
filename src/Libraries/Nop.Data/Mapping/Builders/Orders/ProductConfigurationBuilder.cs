using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Orders
{
    public partial class ProductConfigurationBuilder : NopEntityBuilder<ProductConfiguration>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ProductConfiguration.ProductId)).AsInt32().ForeignKey<ProductConfiguration>().OnDelete(Rule.None)
                .WithColumn(nameof(ProductConfiguration.ComponentId)).AsString(256).NotNullable();
        }
    }
}
