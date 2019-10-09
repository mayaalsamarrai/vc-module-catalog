﻿using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.Model
{
    public class CatalogLanguageEntity : Entity
    {

        [StringLength(64)]
        public string Language { get; set; }

        #region Navigation Properties
        public string CatalogId { get; set; }
        public virtual CatalogEntity Catalog { get; set; }
        #endregion

        public virtual CatalogLanguage ToModel(CatalogLanguage lang)
        {
            if (lang == null)
                throw new ArgumentNullException(nameof(lang));

            lang.Id = this.Id;
            lang.CatalogId = this.Id;
            lang.LanguageCode = this.Language;

            return lang;

        }

        public virtual CatalogLanguageEntity FromModel(CatalogLanguage lang, PrimaryKeyResolvingMap pkMap)
        {
            if (lang == null)
                throw new ArgumentNullException(nameof(lang));

            pkMap.AddPair(lang, this);

            this.Id = lang.Id;
            this.CatalogId = lang.Id;
            this.Language = lang.LanguageCode;


            return this;
        }

        public virtual void Patch(CatalogLanguageEntity target)
        {
            target.Language = this.Language;          
        }
    }
}
