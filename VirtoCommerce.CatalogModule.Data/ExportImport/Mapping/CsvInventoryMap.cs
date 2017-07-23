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
    public sealed class CsvInventoryMap : CsvMapBase<CsvInventory>
    {
        public CsvInventoryMap(CsvProductMappingConfiguration mappingCfg)
            : base(mappingCfg)
        {        
        }
    }
}
