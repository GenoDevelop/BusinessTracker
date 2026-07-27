using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Infrastructure;

namespace GenoDev.BusinessTracker.TestsUtilities.Extensions;

public static class BusinessTrackerDbContextExtensions
{
    extension(BusinessTrackerDbContext db)
    {
        public Supplier Arrange_Supplier(Guid? id = null,
            string name = "Test Supplier",
            string? nip = null,
            string? websiteUrl = null,
            string? description = null)
        {
            var supplier = new Supplier
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Nip = nip,
                WebsiteUrl = websiteUrl,
                Description = description,
                Supplies = []
            };
        
            db.Suppliers.Add(supplier);
            return supplier;
        }

        public Material Arrange_Material(Guid? id = null,
            string name = "Test Material")
        {
            var material = new Material
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                MaterialVariants = [],
                ProductRecipeMaterials = []
            };
        
            db.Materials.Add(material);
            return material;
        }

        public MaterialVariant Arrange_MaterialVariant(Material? material = null,
            Guid? id = null,
            string name = "Test Variant",
            string? ean = null,
            string? manufacturerCode = null,
            string? description = null,
            string unit = "pcs",
            double totalUsedAmount = 0,
            double companyAmount = 0,
            double privateAmount = 0)
        {
            material ??= db.Arrange_Material();

            var variant = new MaterialVariant
            {
                Id = id ?? Guid.NewGuid(),
                MaterialId = material.Id,
                Material = material,
                Name = name,
                Ean = ean,
                ManufacturerCode = manufacturerCode,
                Description = description,
                Unit = unit,
                TotalUsedAmount = totalUsedAmount,
                TotalCompanyAmount = companyAmount,
                TotalPrivateAmount = privateAmount,
                SupplyItems = [],
                ProductionMaterials = []
            };

            variant.Material.MaterialVariants.Add(variant);

            db.MaterialVariants.Add(variant);
            return variant;
        }

        public Supply Arrange_Supply(Supplier? supplier = null,
            Guid? id = null,
            DateTime? orderDate = null,
            string? description = null,
            MaterialSupplyStatus status = MaterialSupplyStatus.Ordered,
            string? invoiceNo = null,
            decimal shippingNetPrice = 0,
            decimal shippingGrossPrice = 0)
        {
            supplier ??= db.Arrange_Supplier();
        
            var supply = new Supply
            {
                Id = id ?? Guid.NewGuid(),
                SupplierId = supplier.Id,
                Supplier = supplier,
                OrderDate = orderDate ?? DateTime.Now,
                Description = description,
                Status = status,
                InvoiceNo = invoiceNo,
                ShippingNetPrice = shippingNetPrice,
                ShippingGrossPrice = shippingGrossPrice,
                SupplyItems = []
            };
        
            supply.Supplier.Supplies.Add(supply);
        
            db.Supplies.Add(supply);
            return supply;
        }

        public SupplyItem Arrange_SupplyItem(Supply? supply = null,
            MaterialVariant? materialVariant = null,
            PackingMaterial? packingMaterial = null,
            FixedAsset? fixedAsset = null,
            Guid? id = null,
            int setsAmount = 1,
            double unitsInSet = 1,
            decimal setNetPrice = 10.0m,
            decimal setGrossPrice = 12.3m,
            bool privateSupply = false)
        {
            supply ??= db.Arrange_Supply();

            StorageItemType itemType;
            if (materialVariant != null) itemType = StorageItemType.Material;
            else if (packingMaterial != null) itemType = StorageItemType.Packing;
            else if (fixedAsset != null) itemType = StorageItemType.FixedAsset;
            else
            {
                itemType = Enum.GetValues<StorageItemType>()
                    .OrderBy(_ => Random.Shared.Next())
                    .First();

                if (itemType == StorageItemType.Material) materialVariant = db.Arrange_MaterialVariant();
                else if (itemType == StorageItemType.Packing) packingMaterial = db.Arrange_PackingMaterial();
                else if (itemType == StorageItemType.FixedAsset) fixedAsset = db.Arrange_FixedAsset();
                else throw new InvalidOperationException("Invalid supply item type");
            }

            var item = new SupplyItem
            {
                Id = id ?? Guid.NewGuid(),
                MaterialSupplyId = supply.Id,
                Supply = supply,
                ItemType = itemType,
                MaterialVariantId = materialVariant?.Id,
                MaterialVariant = materialVariant,
                PackingMaterialId = packingMaterial?.Id,
                PackingMaterial = packingMaterial,
                FixedAssetId = fixedAsset?.Id,
                FixedAsset = fixedAsset,
                SetsAmount = setsAmount,
                UnitsInSet = unitsInSet,
                SetNetPrice = setNetPrice,
                SetGrossPrice = setGrossPrice,
                PrivateSupply = privateSupply
            };

            item.Supply.SupplyItems.Add(item);
            item.MaterialVariant?.SupplyItems.Add(item);
            item.PackingMaterial?.SupplyItems.Add(item);
            item.FixedAsset?.SupplyItems.Add(item);
        
            db.SupplyItems.Add(item);
            return item;
        }

        public FixedAsset Arrange_FixedAsset(Guid? id = null,
            string name = "Test Fixed Asset",
            string? ean = null,
            string? manufacturerCode = null,
            string? description = null,
            string? unit = null,
            double totalCompanyAmount = 0,
            double totalPrivateAmount = 0)
        {
            var asset = new FixedAsset
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Ean = ean,
                ManufacturerCode = manufacturerCode,
                Description = description,
                Unit = unit,
                TotalCompanyAmount = totalCompanyAmount,
                TotalPrivateAmount = totalPrivateAmount,
                SupplyItems = []
            };
            db.FixedAssets.Add(asset);
            return asset;
        }

        public PackingMaterial Arrange_PackingMaterial(Guid? id = null,
            string name = "Test Packing Material",
            string? ean = null,
            string? description = null,
            string unit = "pcs",
            string? manufacturerCode = null,
            double totalUsedAmount = 0,
            double totalCompanyAmount = 0,
            double totalPrivateAmount = 0)
        {
            var packingMaterial = new PackingMaterial
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Ean = ean,
                Description = description,
                Unit = unit,
                ManufacturerCode = manufacturerCode,
                TotalUsedAmount = totalUsedAmount,
                TotalCompanyAmount = totalCompanyAmount,
                TotalPrivateAmount = totalPrivateAmount,
                SupplyItems = [],
                OrderPackingMaterials = []
            };

            db.PackingMaterials.Add(packingMaterial);
            return packingMaterial;
        }

        public Product Arrange_Product(Guid? id = null,
            string name = "Test Product",
            string? description = null,
            string? identifier = null,
            int amount = 0)
        {
            var product = new Product
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = description,
                Identifier = identifier ?? Guid.NewGuid().ToString(),
                Amount = amount,
                ProductRecipes = [],
                Productions = [],
                OrderProducts = []
            };
        
            db.Products.Add(product);
            return product;
        }

        public ProductRecipe Arrange_ProductRecipe(Product? product = null,
            Guid? id = null,
            string name = "Test Recipe",
            string description = "Test Recipe Description")
        {
            product ??= db.Arrange_Product();

            var recipe = new ProductRecipe
            {
                Id = id ?? Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                Name = name,
                Description = description,
                ProductRecipeMaterials = []
            };

            recipe.Product.ProductRecipes.Add(recipe);
        
            db.ProductRecipes.Add(recipe);
            return recipe;
        }

        public ProductRecipeMaterial Arrange_ProductRecipeMaterial(ProductRecipe? productRecipe = null,
            Material? material = null,
            Guid? id = null)
        {
            productRecipe ??= db.Arrange_ProductRecipe();
            material ??= db.Arrange_Material();

            var recipeMaterial = new ProductRecipeMaterial
            {
                Id = id ?? Guid.NewGuid(),
                ProductRecipeId = productRecipe.Id,
                ProductRecipe = productRecipe,
                MaterialId = material.Id,
                Material = material
            };

            recipeMaterial.ProductRecipe.ProductRecipeMaterials.Add(recipeMaterial);
            recipeMaterial.Material.ProductRecipeMaterials.Add(recipeMaterial);
        
            db.ProductRecipeMaterials.Add(recipeMaterial);
            return recipeMaterial;
        }

        public Production Arrange_Production(Product? product = null,
            Guid? id = null,
            DateTime? productionDate = null,
            int amount = 1,
            string? description = null)
        {
            product ??= db.Arrange_Product();

            var production = new Production
            {
                Id = id ?? Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                ProductionDate = productionDate ?? DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Amount = amount,
                Description = description,
                ProductionMaterials = []
            };

            production.Product.Productions.Add(production);
        
            db.Productions.Add(production);
            return production;
        }

        public ProductionMaterial Arrange_ProductionMaterial(Production? production = null,
            MaterialVariant? materialVariant = null,
            Guid? id = null,
            double usedAmount = 1.0)
        {
            production ??= db.Arrange_Production();
            materialVariant ??= db.Arrange_MaterialVariant();

            var productionMaterial = new ProductionMaterial
            {
                Id = id ?? Guid.NewGuid(),
                ProductionId = production.Id,
                Production = production,
                MaterialVariantId = materialVariant.Id,
                MaterialVariant = materialVariant,
                UsedAmount = usedAmount
            };

            productionMaterial.Production.ProductionMaterials.Add(productionMaterial);
            productionMaterial.MaterialVariant.ProductionMaterials.Add(productionMaterial);
        
            db.ProductionMaterials.Add(productionMaterial);
            return productionMaterial;
        }

        public Order Arrange_Order(Guid? id = null,
            string? description = null,
            DateTime? orderDate = null,
            string? orderIdentifier = null,
            string? paymentIdentifier = null,
            string? trackingNumber = null,
            Carrier? carrier = null,
            OrderStatus status = OrderStatus.New)
        {
            var order = new Order
            {
                Id = id ?? Guid.NewGuid(),
                Description = description,
                OrderDate = orderDate ?? DateTime.Now,
                OrderIdentifier = orderIdentifier ?? Guid.NewGuid().ToString(),
                PaymentIdentifier = paymentIdentifier,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                Status = status,
                OrderProducts = [],
                OrderPackingMaterials = []
            };

            db.Orders.Add(order);
            return order;
        }

        public OrderProduct Arrange_OrderProduct(Order? order = null,
            Product? product = null,
            Guid? id = null,
            int orderedAmount = 1,
            int assignedAmount = 0,
            decimal unitNetPrice = 100.0m,
            decimal unitGrossPrice = 123.0m)
        {
            order ??= db.Arrange_Order();
            product ??= db.Arrange_Product();

            var orderProduct = new OrderProduct
            {
                Id = id ?? Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                ProductId = product.Id,
                Product = product,
                OrderedAmount = orderedAmount,
                AssignedAmount = assignedAmount,
                UnitNetPrice = unitNetPrice,
                UnitGrossPrice = unitGrossPrice
            };

            orderProduct.Order.OrderProducts.Add(orderProduct);
            orderProduct.Product.OrderProducts.Add(orderProduct);
        
            db.OrderProducts.Add(orderProduct);
            return orderProduct;
        }

        public OrderPackingMaterial Arrange_OrderPackingMaterial(Order? order = null,
            PackingMaterial? packingMaterial = null,
            Guid? id = null,
            double amount = 1.0)
        {
            order ??= db.Arrange_Order();
            packingMaterial ??= db.Arrange_PackingMaterial();

            var opm = new OrderPackingMaterial
            {
                Id = id ?? Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                PackingMaterialId = packingMaterial.Id,
                PackingMaterial = packingMaterial,
                Amount = amount
            };

            opm.Order.OrderPackingMaterials.Add(opm);
            opm.PackingMaterial.OrderPackingMaterials.Add(opm);

            db.OrderPackingMaterials.Add(opm);
            return opm;
        }
    }
}
