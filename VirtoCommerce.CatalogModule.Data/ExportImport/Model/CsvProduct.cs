using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Inventory.Model;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvProduct : Entity
    {
        public int CsvLineNumber { get; set; }

        public string Name { get; set; }
        public string MainProductId { get; set; }
        public string IsActive { get; set; }
        public string IsBuyable { get; set; }
        public string TrackInventory { get; set; }
        public string PrimaryImage { get; set; }
        public string AltImage { get; set; }
        public string Sku { get; set; }
        public string ParentSku { get; set; }
        public string ReviewType { get; set; }
        public string Review { get; set; }
        public string SeoTitle { get; set; }
        public string SeoUrl { get; set; }
        public string SeoDescription { get; set; }     
        public string Gtin { get; set; }
        public string MeasureUnit { get; set; }
        public string WeightUnit { get; set; }
        public string Weight { get; set; }
        public string Height { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string ProductType { get; set; }
        public string TaxType { get; set; }
        public string ShippingType { get; set; }
        public string Vendor { get; set; }

        public CsvInventory Inventory { get; set; }
        public CsvPrice Price { get; set; }
        public CsvCategory Category { get; set; }
        public ICollection<CsvPropertyValue> PropertyValues { get; set; }

        public virtual CatalogProduct ToModel(CatalogProduct product)
        {
            //TODO
            return product;
        }

        public virtual CsvProduct FomModel(CatalogProduct product)
        {
            //TODO
            return this;
        }

        public virtual void Patch(CatalogProduct product)
        {
            if (!PropertyValues.IsNullOrEmpty())
            {
                //Properties inheritance
                var properties = (product.Category != null ? product.Category.Properties : product.Catalog.Properties).OrderBy(x => x.Name).ToList();
                var propertyValues = this.PropertyValues.Select(x => x.ToModel(new PropertyValue())).ToList();
                foreach (var propertyValue in propertyValues)
                {
                    //Try to find property meta information
                    propertyValue.Property = properties.FirstOrDefault(x => x.Name.EqualsInvariant(propertyValue.PropertyName));
                    if (propertyValue.Property != null)
                    {
                        propertyValue.ValueType = propertyValue.Property.ValueType;
                        if (propertyValue.Property.Dictionary)
                        {
                            var dicValue = propertyValue.Property.DictionaryValues.FirstOrDefault(x => Equals(x.Value, propertyValue.Value));
                            if (dicValue == null)
                            {
                                dicValue = new PropertyDictionaryValue
                                {
                                    Alias = propertyValue.Value.ToString(),
                                    Value = propertyValue.Value.ToString(),
                                    Id = Guid.NewGuid().ToString()
                                };
                            }
                            propertyValue.ValueId = dicValue.Id;
                        }
                    }
                }
                var propertyValueComparer = AnonymousComparer.Create((PropertyValue x) => $"{x.PropertyName}-{x.Value}");
                //Only new values allowed
                propertyValues.CompareTo(product.PropertyValues, propertyValueComparer, (state, sourcePropValue, targetPropValue) =>
                {
                    if (state == EntryState.Added)
                    {
                        product.PropertyValues.Add(sourcePropValue);
                    }
                });
            }
        }

        private void ResolveMetainformation()
        {

        }
    }
}
