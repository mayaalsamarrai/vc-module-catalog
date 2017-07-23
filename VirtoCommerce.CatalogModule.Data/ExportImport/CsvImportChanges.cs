using ChangeDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Pricing.Model;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvImportChanges
    {
        public IElementChangeCollection<Category> CategoriesChanges { get; set; }
        public IElementChangeCollection<CatalogProduct> ProductChanges { get; set; }
        public IElementChangeCollection<Property>  PropertyChanges { get; set; }
        public IElementChangeCollection<Price> PriceChanges { get; set; }
    }
}
