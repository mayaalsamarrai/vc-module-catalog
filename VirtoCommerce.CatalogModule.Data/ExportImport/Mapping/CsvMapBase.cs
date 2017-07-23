using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.CatalogModule.Data.ExportImport.Mapping
{
    public abstract class CsvMapBase<T> : CsvClassMap<T>
    {
        public CsvMapBase(CsvProductMappingConfiguration mappingCfg)
        {
            //Dynamical map scalar product fields use by manual mapping information
            foreach (var mappingItem in mappingCfg.PropertyMaps.Where(x => !string.IsNullOrEmpty(x.CsvColumnName) || !string.IsNullOrEmpty(x.CustomValue)))
            {
                var propertyInfo = typeof(T).GetProperty(mappingItem.EntityColumnName);
                if (propertyInfo != null)
                {
                    var newMap = new CsvPropertyMap(propertyInfo);
                    newMap.TypeConverterOption(CultureInfo.InvariantCulture);
                    newMap.TypeConverterOption(NumberStyles.Any);
                    newMap.TypeConverterOption(true, "yes", "true");
                    newMap.TypeConverterOption(false, "false", "no");
                    if (!string.IsNullOrEmpty(mappingItem.CsvColumnName))
                    {
                        //Map fields if mapping specified
                        newMap.Name(mappingItem.CsvColumnName);
                    }
                    //And default values if it specified
                    if (mappingItem.CustomValue != null)
                    {
                        var typeConverter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                        newMap.Default(typeConverter.ConvertFromString(mappingItem.CustomValue));
                    }
                    PropertyMaps.Add(newMap);
                }
            }           
        }
    }
}
