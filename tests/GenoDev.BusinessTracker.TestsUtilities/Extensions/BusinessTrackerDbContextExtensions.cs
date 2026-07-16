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
            string? description = null,
            string? websiteUrl = null)
        {
            var supplier = new Supplier
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Nip = nip,
                Description = description,
                WebsiteUrl = websiteUrl,
                MaterialSupplies = []
            };
        
            db.Suppliers.Add(supplier);
            return supplier;
        }

        public Material Arrange_Material(Guid? id = null,
            string name = "Test Material",
            string? ean = null,
            string? description = null,
            string unit = "pcs",
            double amount = 0)
        {
            var material = new Material
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Ean = ean,
                Description = description,
                Unit = unit,
                Amount = amount,
                MaterialSupplyItems = [],
                ProductRecipeMaterials = [],
                ProductionMaterials = []
            };
        
            db.Materials.Add(material);
            return material;
        }

        public MaterialSupply Arrange_MaterialSupply(Supplier? supplier = null,
            Guid? id = null,
            DateTime? orderDate = null,
            string? description = null,
            MaterialSupplyStatus status = MaterialSupplyStatus.Ordered)
        {
            supplier ??= db.Arrange_Supplier();
        
            var materialSupply = new MaterialSupply
            {
                Id = id ?? Guid.NewGuid(),
                SupplierId = supplier.Id,
                Supplier = supplier,
                OrderDate = orderDate ?? DateTime.Now,
                Description = description,
                Status = status,
                MaterialSupplyItems = []
            };
        
            materialSupply.Supplier.MaterialSupplies.Add(materialSupply);
        
            db.MaterialSupplies.Add(materialSupply);
            return materialSupply;
        }

        public MaterialSupplyItem Arrange_MaterialSupplyItem(MaterialSupply? materialSupply = null,
            Material? material = null,
            Guid? id = null,
            int setsAmount = 1,
            double unitsInSet = 1,
            decimal setNetPrice = 10.0m,
            decimal setGrossPrice = 12.3m)
        {
            materialSupply ??= db.Arrange_MaterialSupply();
            material ??= db.Arrange_Material();

            var item = new MaterialSupplyItem
            {
                Id = id ?? Guid.NewGuid(),
                MaterialSupplyId = materialSupply.Id,
                MaterialSupply = materialSupply,
                MaterialId = material.Id,
                Material = material,
                SetsAmount = setsAmount,
                UnitsInSet = unitsInSet,
                SetNetPrice = setNetPrice,
                SetGrossPrice = setGrossPrice
            };

            item.MaterialSupply.MaterialSupplyItems.Add(item);
            item.Material.MaterialSupplyItems.Add(item);
        
            db.MaterialSupplyItems.Add(item);
            return item;
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
            Guid? id = null,
            double requiredAmount = 1.0)
        {
            productRecipe ??= db.Arrange_ProductRecipe();
            material ??= db.Arrange_Material();

            var recipeMaterial = new ProductRecipeMaterial
            {
                Id = id ?? Guid.NewGuid(),
                ProductRecipeId = productRecipe.Id,
                ProductRecipe = productRecipe,
                MaterialId = material.Id,
                Material = material,
                RequiredAmount = requiredAmount
            };

            recipeMaterial.ProductRecipe.ProductRecipeMaterials.Add(recipeMaterial);
            recipeMaterial.Material.ProductRecipeMaterials.Add(recipeMaterial);
        
            db.ProductRecipeMaterials.Add(recipeMaterial);
            return recipeMaterial;
        }

        public Production Arrange_Production(Product? product = null,
            Guid? id = null,
            DateTime? productionDate = null,
            string? description = null)
        {
            product ??= db.Arrange_Product();

            var production = new Production
            {
                Id = id ?? Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                ProductionDate = productionDate ?? DateTime.Now,
                Description = description,
                ProductionMaterials = []
            };

            production.Product.Productions.Add(production);
        
            db.Productions.Add(production);
            return production;
        }

        public ProductionMaterial Arrange_ProductionMaterial(Production? production = null,
            Material? material = null,
            Guid? id = null,
            double usedAmount = 1.0)
        {
            production ??= db.Arrange_Production();
            material ??= db.Arrange_Material();

            var productionMaterial = new ProductionMaterial
            {
                Id = id ?? Guid.NewGuid(),
                ProductionId = production.Id,
                Production = production,
                MaterialId = material.Id,
                Material = material,
                UsedAmount = usedAmount
            };

            productionMaterial.Production.ProductionMaterials.Add(productionMaterial);
            productionMaterial.Material.ProductionMaterials.Add(productionMaterial);
        
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
                OrderProducts = []
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
    }
}
