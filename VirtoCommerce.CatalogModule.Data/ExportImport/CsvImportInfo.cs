namespace VirtoCommerce.CatalogModule.Data.ExportImport
{
    public class CsvImportInfo
    {
        public string CatalogId { get; set; }
        public string FileUrl { get; set; }
        public CsvProductMappingConfiguration Configuration { get; set; }
    }
}
