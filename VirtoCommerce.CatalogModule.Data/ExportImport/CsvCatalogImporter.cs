using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Omu.ValueInjecter;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Inventory.Services;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.CatalogModule.Data.ExportImport.Mapping;
using ChangeDetector;
using VirtoCommerce.CatalogModule.Data.Services.Validation;
using FluentValidation.Results;
using FluentValidation;
using VirtoCommerce.Domain.Pricing.Model;

namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public sealed class CsvCatalogImporter
    {
 
        private readonly ICatalogService _catalogService;
        private readonly ICategoryService _categoryService;
        private readonly IItemService _productService;
        private readonly ISkuGenerator _skuGenerator;
        private readonly IPricingService _pricingService;
        private readonly IPricingSearchService _pricingSearchService;
        private readonly IInventoryService _inventoryService;
        private readonly ICommerceService _commerceService;
        private readonly IPropertyService _propertyService;
        private readonly ICatalogSearchService _searchService;
        private readonly Func<ICatalogRepository> _catalogRepositoryFactory;
        private readonly object _lockObject = new object();

        public CsvCatalogImporter(ICatalogService catalogService, ICategoryService categoryService, IItemService productService,
                                  ISkuGenerator skuGenerator,
                                  IPricingService pricingService, IInventoryService inventoryService, ICommerceService commerceService,
                                  IPropertyService propertyService, ICatalogSearchService searchService, Func<ICatalogRepository> catalogRepositoryFactory, IPricingSearchService pricingSearchService)
        {
            _catalogService = catalogService;
            _categoryService = categoryService;
            _productService = productService;
            _skuGenerator = skuGenerator;
            _pricingService = pricingService;
            _inventoryService = inventoryService;
            _commerceService = commerceService;
            _propertyService = propertyService;
            _searchService = searchService;
            _catalogRepositoryFactory = catalogRepositoryFactory;
            _pricingSearchService = pricingSearchService;
        }

        public IEnumerable<CsvProduct> LoadCsvProducts(Stream inputStream, CsvImportInfo importInfo, Action<ExportImportProgressInfo> progressCallback)
        {
            var result = new List<CsvProduct>();

            var progressInfo = new ExportImportProgressInfo
            {
                Description = "Reading products from csv..."
            };
            progressCallback(progressInfo);

            using (var reader = new CsvReader(new StreamReader(inputStream)))
            {
                reader.Configuration.Delimiter = importInfo.Configuration.Delimiter;
                reader.Configuration.RegisterClassMap(new CsvProductMap(importInfo.Configuration));
                reader.Configuration.WillThrowOnMissingField = false;

                while (reader.Read())
                {
                    try
                    {
                        var csvProduct = reader.GetRecord<CsvProduct>();
                        csvProduct.CsvLineNumber = reader.Row;
                        result.Add(csvProduct);
                    }
                    catch (Exception ex)
                    {
                        var error = ex.Message;
                        if (ex.Data.Contains("CsvHelper"))
                        {
                            error += ex.Data["CsvHelper"];
                        }
                        progressInfo.Errors.Add(error);
                        progressCallback(progressInfo);
                    }
                }
            }
            return result;
        }

        public CsvImportChanges DetectChanges(IEnumerable<CsvProduct> csvProducts, CsvImportInfo importInfo, Action<ExportImportProgressInfo> progressCallback)
        {
            var result = new CsvImportChanges();

            var catalog = _catalogService.GetById(importInfo.CatalogId);
            //Detect changes
            result.CategoriesChanges = DetectCategoriesChanges(catalog, csvProducts.Select(x => x.Category));
            result.ProductChanges = DetectProductChanges(catalog, csvProducts);

            //Validation
            var newCategories = result.CategoriesChanges.Where(x => x.State == ElementState.Added).Select(x => x.Item).ToArray();
            var categoryValidator = new CategoryValidator();
            foreach (var category in newCategories)
            {
                categoryValidator.ValidateAndThrow(category);
            }

            var products = result.ProductChanges.Where(x => x.State == ElementState.Added || x.State == ElementState.Unmodified).Select(x => x.Item).ToArray();
            var productValidator = new ProductValidator();
            foreach (var product in products)
            {
                productValidator.ValidateAndThrow(product);
            }

            return result;
        }

        public void DoImport(IEnumerable<CsvProduct> csvProducts, CsvImportInfo importInfo, Action<ExportImportProgressInfo> progressCallback)
        {
            //SaveProducts(catalog, csvProducts, progressInfo, progressCallback);
        }


        private IElementChangeCollection<Property> DetectPropertyChanges(Catalog catalog, IEnumerable<CsvProduct> csvProducts)
        {
            var alreadyExistProperties = _propertyService.GetAllCatalogProperties(catalog.Id);
            var changedProperties = new List<Property>();
            foreach (var csvProduct in csvProducts.Where(x => !x.PropertyValues.IsNullOrEmpty()))
            {
                foreach(var propertyValue in csvProduct.PropertyValues)
                {
                    var property = alreadyExistProperties.FirstOrDefault(x => x.Name.EqualsInvariant(propertyValue.PropertyName));
                    if (property != null && property.Dictionary && !property.DictionaryValues.Any(x => x.Value.EqualsInvariant(propertyValue.Value)))
                    {
                        property.DictionaryValues.Add(new PropertyDictionaryValue { Value = propertyValue.Value, Alias = propertyValue.Value });
                        if (!changedProperties.Contains(property))
                        {
                            changedProperties.Add(property);
                        }
                    }
                }
            }
        }

        private IElementChangeCollection<CatalogProduct> DetectProductChanges(Catalog catalog, IEnumerable<CsvProduct> csvProducts)
        {
            var alreadyExistProducts = new List<CatalogProduct>();
      
            var transientProducts = csvProducts.Where(x => x.IsTransient()).ToArray();
            var nonTransientProducts = csvProducts.Where(x => !x.IsTransient()).ToArray();
            //Load exist products
            for (int i = 0; i < nonTransientProducts.Count(); i += 50)
            {
                alreadyExistProducts.AddRange(_productService.GetByIds(nonTransientProducts.Skip(i).Take(50).Select(x => x.Id).ToArray(), ItemResponseGroup.ItemLarge));
            }
            //Detect already exist product by Code
            var transientProductsCodes = transientProducts.Select(x => x.Sku).Where(x => x != null).Distinct().ToArray();
            using (var repository = _catalogRepositoryFactory())
            {
                var foundProducts = repository.Items.Where(x => x.CatalogId == catalog.Id && transientProductsCodes.Contains(x.Code)).Select(x => new { Id = x.Id, Code = x.Code }).ToArray();
                for (int i = 0; i < foundProducts.Count(); i += 50)
                {
                    alreadyExistProducts.AddRange(_productService.GetByIds(foundProducts.Skip(i).Take(50).Select(x => x.Id).ToArray(), ItemResponseGroup.ItemLarge));
                }
            }
            var changedProducts = new List<CatalogProduct>();
            foreach(var csvProduct in csvProducts)
            {
                var existProduct = alreadyExistProducts.FirstOrDefault(x => csvProduct.IsTransient() ? x.Code.EqualsInvariant(csvProduct.Sku) : x.Id.EqualsInvariant(csvProduct.Id));
                if (existProduct != null)
                {
                    csvProduct.Patch(existProduct);
                }
                else
                {
                    var newProduct = new CatalogProduct();
                    newProduct.Catalog = catalog;                  
                    existProduct = csvProduct.ToModel(newProduct);
                    existProduct.CatalogId = catalog.Id;
                }
                changedProducts.Add(existProduct);
            }
            var changeDetector = new CollectionChangeDetector<CatalogProduct>();
            return changeDetector.GetChanges(alreadyExistProducts, changedProducts, ElementState.Added | ElementState.Unmodified);
        }

        private IElementChangeCollection<Category> DetectCategoriesChanges(Catalog catalog, IEnumerable<CsvCategory> csvCategories)
        {
            var allCategoriesCriteria = new SearchCriteria
            {
                CatalogId = catalog.Id,
                SearchInChildren = true,
                ResponseGroup = SearchResponseGroup.WithCategories,
                Take = int.MaxValue
            };
            var existCategoriesMap = _searchService.Search(allCategoriesCriteria).Categories.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var changedCategoriesMap = new Dictionary<string, Category>();
            //Try to create categories tree by path from transient categories
            foreach (var csvCategory in csvCategories.OrderBy(x => x.Outlines.Count()))
            {
                if (!string.IsNullOrEmpty(csvCategory.CategoryId))
                {
                    changedCategoriesMap[csvCategory.CategoryId] = csvCategory.ToModel(new Category());
                }
                else
                {
                    var parentId = string.Empty;
                    foreach(var categoryId in csvCategory.Outlines)
                    {
                        if (!changedCategoriesMap.ContainsKey(categoryId))
                        {
                            var category = csvCategory.ToModel(new Category());
                            category.ParentId = parentId;
                            changedCategoriesMap[categoryId] = category;
                        }
                        parentId = categoryId;
                    }
                }
            }
            var changeDetector = new CollectionChangeDetector<Category>();
            return changeDetector.GetChanges(existCategoriesMap.Values.ToList(), changedCategoriesMap.Values.ToList(), ElementState.Added);
        }

        private IElementChangeCollection<Price> DetectPriceChanges(Catalog catalog, IEnumerable<CsvPrice> csvPrices)
        {                      
        }

        //private void SaveProducts(Catalog catalog, List<CsvProduct> csvProducts, ExportImportProgressInfo progressInfo, Action<ExportImportProgressInfo> progressCallback)
        //{
        //    progressInfo.ProcessedCount = 0;
        //    progressInfo.TotalCount = csvProducts.Count;

        //    var defaultFulfilmentCenter = _commerceService.GetAllFulfillmentCenters().FirstOrDefault();

        //    ICollection<Property> modifiedProperties;
        //    LoadProductDependencies(csvProducts, catalog, out modifiedProperties);
        //    MergeFromAlreadyExistProducts(csvProducts, catalog);

        //    var defaultLanguge = catalog.DefaultLanguage != null ? catalog.DefaultLanguage.LanguageCode : "en-US";           
        //    foreach (var csvProduct in csvProducts)
        //    {
        //        //Try to detect and split single property value in to multiple values for multivalue properties 
        //        //csvProduct.PropertyValues = TryToSplitMultivaluePropertyValues(csvProduct);
        //    }

        //    progressInfo.Description = string.Format("Saving property dictionary values...");
        //    progressCallback(progressInfo);
        //    _propertyService.Update(modifiedProperties.ToArray());

        //    var totalProductsCount = csvProducts.Count();
        //    //Order to save main products first then variations
        //    csvProducts = csvProducts.OrderBy(x => x.MainProductId != null).ToList();
        //    for (int i = 0; i < totalProductsCount; i += 10)
        //    {
        //        var products = csvProducts.Skip(i).Take(10);
        //        try
        //        {
        //            //Save main products first and then variations
        //            _productService.Update(products.ToArray());

        //            //Set productId for dependent objects
        //            foreach (var product in products)
        //            {
        //                if (defaultFulfilmentCenter != null || product.Inventory.FulfillmentCenterId != null)
        //                {
        //                    product.Inventory.ProductId = product.Id;
        //                    product.Inventory.FulfillmentCenterId = product.Inventory.FulfillmentCenterId ?? defaultFulfilmentCenter.Id;
        //                    product.Price.ProductId = product.Id;
        //                }
        //                else
        //                {
        //                    product.Inventory = null;
        //                }
        //            }
        //            var productIds = products.Select(x => x.Id).ToArray();
        //            var existInventories = _inventoryService.GetProductsInventoryInfos(productIds);
        //            var inventories = products.Where(x => x.Inventory != null).Select(x => x.Inventory).ToArray();
        //            foreach (var inventory in inventories)
        //            {
        //                var exitsInventory = existInventories.FirstOrDefault(x => x.ProductId == inventory.ProductId && x.FulfillmentCenterId == inventory.FulfillmentCenterId);
        //                if (exitsInventory != null)
        //                {
        //                    inventory.InjectFrom(exitsInventory);
        //                }
        //            }
        //            _inventoryService.UpsertInventories(inventories);

        //            //We do not have information about concrete price list id and therefore select first product price then
        //            var existPrices = _pricingSearchService.SearchPrices(new Domain.Pricing.Model.Search.PricesSearchCriteria { ProductIds = productIds }).Results;
        //            var prices = products.Where(x => x.Price != null && x.Price.EffectiveValue > 0).Select(x => x.Price).ToArray();
        //            foreach (var price in prices)
        //            {
        //                var existPrice = existPrices.FirstOrDefault(x => x.Currency.EqualsInvariant(price.Currency) && x.ProductId.EqualsInvariant(price.ProductId));
        //                if (existPrice != null)
        //                {
        //                    price.InjectFrom(existPrice);
        //                }
        //            }
        //            _pricingService.SavePrices(prices);
        //        }
        //        catch (Exception ex)
        //        {
        //            lock (_lockObject)
        //            {
        //                progressInfo.Errors.Add(ex.ToString());
        //                progressCallback(progressInfo);
        //            }
        //        }
        //        finally
        //        {
        //            lock (_lockObject)
        //            {
        //                //Raise notification
        //                progressInfo.ProcessedCount += products.Count();
        //                progressInfo.Description = string.Format("Saving products: {0} of {1} created", progressInfo.ProcessedCount, progressInfo.TotalCount);
        //                progressCallback(progressInfo);
        //            }
        //        }
        //    }
        //}

        //private List<PropertyValue> TryToSplitMultivaluePropertyValues(CsvProduct csvProduct)
        //{
        //    var result = new List<PropertyValue>();
        //    //Try to split multivalues
        //    foreach (var propValue in csvProduct.PropertyValues)
        //    {
        //        if (propValue.Value != null && propValue.Property != null && propValue.Property.Multivalue)
        //        {
        //            var values = propValue.Value.ToString().Split(',', ';');
        //            foreach (var value in values)
        //            {
        //                var multiPropValue = propValue.Clone() as PropertyValue;
        //                multiPropValue.Value = value;
        //                result.Add(multiPropValue);
        //            }
        //        }
        //        else
        //        {
        //            result.Add(propValue);
        //        }
        //    }
        //    return result;
        //}    

        //private void LoadProductDependencies(IEnumerable<CsvProduct> csvProducts, Catalog catalog, out ICollection<Property> modifiedProperties)
        //{
        //    modifiedProperties = new List<Property>();
        //    var allCategoriesIds = csvProducts.Select(x => x.CategoryId).Distinct().ToArray();
        //    var categoriesMap = _categoryService.GetByIds(allCategoriesIds, CategoryResponseGroup.Full).ToDictionary(x => x.Id);
        //    var defaultLanguge = catalog.DefaultLanguage != null ? catalog.DefaultLanguage.LanguageCode : "en-US";

        //    foreach (var csvProduct in csvProducts)
        //    {
        //        csvProduct.Catalog = catalog;
        //        csvProduct.CatalogId = catalog.Id;
        //        if (csvProduct.CategoryId != null)
        //        {
        //            csvProduct.Category = categoriesMap[csvProduct.CategoryId];
        //        }

        //        //Try to set parent relations
        //        //By id or code reference
        //        var parentProduct = csvProducts.FirstOrDefault(x => csvProduct.MainProductId != null && (x.Id == csvProduct.MainProductId || x.Code == csvProduct.MainProductId));
        //        csvProduct.MainProduct = parentProduct;
        //        csvProduct.MainProductId = parentProduct != null ? parentProduct.Id : null;

        //        if (string.IsNullOrEmpty(csvProduct.Code))
        //        {
        //            csvProduct.Code = _skuGenerator.GenerateSku(csvProduct);
        //        }
        //        csvProduct.EditorialReview.LanguageCode = defaultLanguge;
        //        csvProduct.SeoInfo.LanguageCode = defaultLanguge;
        //        csvProduct.SeoInfo.SemanticUrl = string.IsNullOrEmpty(csvProduct.SeoInfo.SemanticUrl) ? csvProduct.Code : csvProduct.SeoInfo.SemanticUrl;

        //        //Properties inheritance
        //        csvProduct.Properties = (csvProduct.Category != null ? csvProduct.Category.Properties : csvProduct.Catalog.Properties).OrderBy(x => x.Name).ToList();
        //        foreach (var propertyValue in csvProduct.PropertyValues.ToArray())
        //        {
        //            //Try to find property meta information
        //            propertyValue.Property = csvProduct.Properties.FirstOrDefault(x => x.Name.EqualsInvariant(propertyValue.PropertyName));
        //            if(propertyValue.Property != null)
        //            {
        //                propertyValue.ValueType = propertyValue.Property.ValueType;
        //                if (propertyValue.Property.Dictionary)
        //                {
        //                    var dicValue = propertyValue.Property.DictionaryValues.FirstOrDefault(x => Equals(x.Value, propertyValue.Value));
        //                    if (dicValue == null)
        //                    {
        //                        dicValue = new PropertyDictionaryValue
        //                        {
        //                            Alias = propertyValue.Value.ToString(),
        //                            Value = propertyValue.Value.ToString(),
        //                            Id = Guid.NewGuid().ToString()
        //                        };
        //                        //need to register modified property for future update
        //                        if (!modifiedProperties.Contains(propertyValue.Property))
        //                        {
        //                            modifiedProperties.Add(propertyValue.Property);
        //                        }
        //                    }
        //                    propertyValue.ValueId = dicValue.Id;
        //                }
        //            }
        //        }
        //    }
        //}

        //Merge importing products with already exist to prevent erasing already exist data, import should only update or create data
        //private void MergeFromAlreadyExistProducts(IEnumerable<CsvProduct> csvProducts, Catalog catalog)
        //{
        //    var transientProducts = csvProducts.Where(x => x.IsTransient()).ToArray();
        //    var nonTransientProducts = csvProducts.Where(x => !x.IsTransient()).ToArray();

        //    var alreadyExistProducts = new List<CatalogProduct>();
        //    //Load exist products
        //    for (int i = 0; i < nonTransientProducts.Count(); i += 50)
        //    {
        //        alreadyExistProducts.AddRange(_productService.GetByIds(nonTransientProducts.Skip(i).Take(50).Select(x => x.Id).ToArray(), ItemResponseGroup.ItemLarge));
        //    }
        //    //Detect already exist product by Code
        //    var transientProductsCodes = transientProducts.Select(x => x.Code).Where(x => x != null).Distinct().ToArray();
        //    using (var repository = _catalogRepositoryFactory())
        //    {
        //        var foundProducts = repository.Items.Where(x => x.CatalogId == catalog.Id && transientProductsCodes.Contains(x.Code)).Select(x => new { Id = x.Id, Code = x.Code }).ToArray();
        //        for (int i = 0; i < foundProducts.Count(); i += 50)
        //        {
        //            alreadyExistProducts.AddRange(_productService.GetByIds(foundProducts.Skip(i).Take(50).Select(x => x.Id).ToArray(), ItemResponseGroup.ItemLarge));
        //        }
        //    }
        //    foreach(var csvProduct in csvProducts)
        //    {
        //        var existProduct = csvProduct.IsTransient() ? alreadyExistProducts.FirstOrDefault(x => x.Code.EqualsInvariant(csvProduct.Code)) : alreadyExistProducts.FirstOrDefault(x=> x.Id == csvProduct.Id);
        //        if(existProduct != null)
        //        {
        //            csvProduct.MergeFrom(existProduct);
        //        }
        //    }           

        //}
    }
}
