using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CsvHelper.Configuration;
using coreModel = VirtoCommerce.Domain.Catalog.Model;
using CsvHelper.TypeConversion;
using System.ComponentModel;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.ExportImport.Mapping
{
    public class CsvProductMap : CsvMapBase<CsvProduct>
    {
        public CsvProductMap(CsvProductMappingConfiguration mappingCfg)
            : base(mappingCfg)
        {
            References<CsvInventoryMap>(m => m.Inventory, mappingCfg);
            References<CsvPriceMap>(m => m.Price, mappingCfg);
            References<CsvCategoryMap>(m => m.Category, mappingCfg);

            //Map properties
            if (!mappingCfg.PropertyCsvColumns.IsNullOrEmpty())
            {
                // Exporting multiple csv fields from the same property (which is a collection)
                foreach (var propertyCsvColumn in mappingCfg.PropertyCsvColumns)
                {
                    // create CsvPropertyMap manually, because this.Map(x =>...) does not allow
                    // to export multiple entries for the same property
                    var csvPropertyMap = new CsvPropertyMap(typeof(CsvProduct).GetProperty("PropertyValues"));
                    csvPropertyMap.Name(propertyCsvColumn);

                    // create custom converter instance which will get the required record from the collection
                    csvPropertyMap.UsingExpression<ICollection<CsvPropertyValue>>(null, propValues =>
                    {
                        var propValue = propValues.FirstOrDefault(x => x.PropertyName == propertyCsvColumn);
                        if (propValue != null)
                        {
                            return propValue.Value != null ? propValue.Value.ToString() : string.Empty;
                        }
                        return string.Empty;
                    });

                    PropertyMaps.Add(csvPropertyMap);
                }

                var propValuesName = ReflectionUtility.GetPropertyName<CsvProduct>(x => x.PropertyValues);
                var newPropMap = new CsvPropertyMap(typeof(CsvProduct).GetProperty(propValuesName));
                newPropMap.UsingExpression<ICollection<CsvPropertyValue>>(null, null).ConvertUsing(x => mappingCfg.PropertyCsvColumns.Select(column => new CsvPropertyValue { PropertyName = column, Value = x.GetField<string>(column) }).ToList());
                PropertyMaps.Add(newPropMap);
            }
        }
    }
}
