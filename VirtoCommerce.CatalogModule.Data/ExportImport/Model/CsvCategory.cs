using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvCategory
    {
        private readonly char[] _categoryDelimiters = { '/', '|', '\\', '>' };

        public string CategoryId { get; set; }

        public string CategoryName
        {
            get
            {
                return Outlines.LastOrDefault();
            }
        }

        public string[] Outlines
        {
            get
            {
                return CategoryPath != null ? CategoryPath.Split(_categoryDelimiters) : new string[] { };
            }
        }
        public string CategoryPath { get; set; }

        public virtual Category ToModel(Category category)
        {
            category.Id = CategoryId;
            category.Name = CategoryName;
            return category;
        }

        public virtual CsvCategory FomModel(Category category)
        {
            CategoryId = category.Id;
            return this;
        }

        public virtual void Patch(Category category)
        {
        }

    }
}
