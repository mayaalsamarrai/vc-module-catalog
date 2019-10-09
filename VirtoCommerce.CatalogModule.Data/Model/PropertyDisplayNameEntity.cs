﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.CatalogModule.Data.Model
{
    public class PropertyDisplayNameEntity : Entity
    {
        [StringLength(64)]
        public string Locale { get; set; }
        [StringLength(512)]
        public string Name { get; set; }

        #region Navigation Properties
        public string PropertyId { get; set; }
        public virtual PropertyEntity Property { get; set; }
        #endregion

        public virtual PropertyDisplayName ToModel(PropertyDisplayName displayName)
        {
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));

            displayName.LanguageCode = this.Locale;
            displayName.Name = this.Name;

            return displayName;
        }

        public virtual PropertyDisplayNameEntity FromModel(PropertyDisplayName displayName)
        {
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));

            this.Locale = displayName.LanguageCode;
            this.Name = displayName.Name;

            return this;
        }

        public virtual void Patch(PropertyDisplayNameEntity target)
        {
           
        }
    }
}
