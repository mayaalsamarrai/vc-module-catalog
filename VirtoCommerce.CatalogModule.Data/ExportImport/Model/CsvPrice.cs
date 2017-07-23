using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvPrice
    {
        public string PriceId { get; set; }
        public string Currency { get; set; }
        public string ProductId { get; set; }
        public string SalePrice { get; set; }
        public string ListPrice { get; set; }

        public virtual Price ToModel(Price price)
        {
            price.Id = this.PriceId;
            price.Currency = this.Currency;
            price.ProductId = this.ProductId;
            price.Sale = string.IsNullOrEmpty(SalePrice) ? 0 : Convert.ToDecimal(SalePrice, CultureInfo.InvariantCulture);
            price.List = string.IsNullOrEmpty(ListPrice) ? 0 : Convert.ToDecimal(ListPrice, CultureInfo.InvariantCulture);
            return price;
        }

        public virtual CsvPrice FomModel(Price price)
        {
            this.PriceId = price.Id;
            this.Currency = price.Currency;
            this.ProductId = price.ProductId;
            this.SalePrice = price.Sale?.ToString(CultureInfo.InvariantCulture);
            this.ListPrice = price.List.ToString(CultureInfo.InvariantCulture);
            return this;
        }

        public virtual void Patch(Price price)
        {

        }
    }
}
