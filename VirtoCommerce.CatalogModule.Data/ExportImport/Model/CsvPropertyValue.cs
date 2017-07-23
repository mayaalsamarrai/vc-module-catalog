using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvPropertyValue
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }

        public virtual PropertyValue ToModel(PropertyValue propValue)
        {
            propValue.PropertyName = PropertyName;
            propValue.Value = Value;
            //TODO
            return propValue;
        }

        public virtual CsvPropertyValue FomModel(PropertyValue propValue)
        {
            //TODO
            return this;
        }

    }
}
