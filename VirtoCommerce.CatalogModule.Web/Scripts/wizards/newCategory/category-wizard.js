angular.module('virtoCommerce.catalogModule')
    .controller('virtoCommerce.catalogModule.newCategoryWizardController', ['$scope', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'virtoCommerce.catalogModule.categories', 'virtoCommerce.catalogModule.listEntries', function ($scope, bladeNavigationService, dialogService, categories, listEntries) {
        var blade = $scope.blade;
        blade.passNameToCode = true;

        var pattern = /[$+;=%{}[\]|\\\/@ ~!^*&()?:'<>,]/g;
        $scope.codeValidator = function (value) {            
            return !pattern.test(value);
        };

        $scope.nameChanged = function () {
            if (blade.passNameToCode) {
                blade.currentEntity.code = blade.currentEntity.name.replace(pattern, "-");
            }
        };

        $scope.saveChanges = function () {
            blade.isLoading = true;

            listEntries.listitemssearch(
                {
                    catalogId: blade.currentEntity.catalogId,
                    categoryId: blade.currentEntity.categoryId,
                    keyword: blade.currentEntity.name,
                    responseGroup: 'withCategories',
                    sort: 'name:asc',
                    // take: 20 //?
                },
                function (data) {
                    if (_.any(data.listEntries, function (x) { return x.name === blade.currentEntity.name })) {
                        blade.isLoading = false;
                        dialogService.showConfirmationDialog({
                            id: "confirmCreate",
                            title: "catalog.dialogs.category-duplicate.title",
                            message: "catalog.dialogs.category-duplicate.message",
                            callback: function (confirmed) {
                                if (confirmed) {
                                    blade.isLoading = true;
                                    createEntity();
                                } else {
                                    $scope.bladeClose();
                                }
                            }
                        });
                    } else {
                        createEntity();
                    }
                });
        };

        function createEntity() {
            blade.currentEntity.$update(null, function (data) {
                $scope.bladeClose(function () {
                    var categoryListBlade = blade.parentBlade;
                    categoryListBlade.setSelectedItem(data);
                    categoryListBlade.showCategoryBlade(data);
                    categoryListBlade.refresh();
                });
            });
        }

        $scope.openBlade = function (type) {
            blade.onClose(function () {
                var newBlade = null;
                switch (type) {
                    case 'properties':
                        newBlade = {
                            id: "categoryPropertyDetail",
                            entityType: "product",
                            currentEntity: blade.currentEntity,
                            propGroups: [{ title: 'catalog.properties.category', type: 'Category' }, { title: 'catalog.properties.product', type: 'Product' }, { title: 'catalog.properties.variation', type: 'Variation' }],
                            controller: 'virtoCommerce.catalogModule.propertyListController',
                            template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/property-list.tpl.html'
                        };
                        break;
                    case 'seo':
                        newBlade = {
                            id: "seo",
                            controller: 'virtoCommerce.coreModule.seo.storeListController',
                            template: 'Modules/$(VirtoCommerce.Core)/Scripts/SEO/blades/seo-detail.tpl.html'
                        };
                        break;
                }

                if (newBlade != null) {
                    bladeNavigationService.showBlade(newBlade, blade);
                }
            });
        };

        $scope.setForm = function (form) { $scope.formScope = form; };

        blade.isLoading = false;
    }]);
