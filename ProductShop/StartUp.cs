namespace ProductShop
{
    using Data;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using ProductShop.Dtos.Export;
    using ProductShop.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class StartUp
    {
        public static void Main(string[] args)
        {
            var context = new ProductShopContext();
            //context.Database.EnsureDeleted();
            //context.Database.EnsureCreated();
            //var inputJson = File.ReadAllText(@"../../../Datasets/categories-products.json");
            //var inputJson = File.ReadAllText(@"../../../Datasets/categories.json");
            //var inputJson = File.ReadAllText(@"../../../Datasets/products.json");
            //var inputJson = File.ReadAllText(@"../../../Datasets/users.json");
            //var result = ImportCategoryProducts(context, inputJson);
            //Console.WriteLine(result);

            Console.WriteLine(GetUsersWithProducts(context));
        }


        public static string ImportCategoryProducts(ProductShopContext context, string inputJson)
        {
            var validCategories = new HashSet<int>(
                context
                .Categories
                .Select(c=>c.Id));

            var validProducts = new HashSet<int>(
                context
                .Products
                .Select(p => p.Id));

            var categoriesProducts = JsonConvert.DeserializeObject<CategoryProduct[]>(inputJson);
            var validEntities = new List<CategoryProduct>();

            foreach (var categoryProduct in categoriesProducts)
            {
                bool isValid = validCategories.Contains(categoryProduct.CategoryId) &&
                    validProducts.Contains(categoryProduct.ProductId);

                if (isValid)
                {
                    validEntities.Add(categoryProduct);
                }

            }

            context.CategoryProducts.AddRange(validEntities);
            context.SaveChanges();

            return $"Successfully imported {validEntities.Count}";
        }

        public static string ImportCategories(ProductShopContext context, string inputJson)
        {
            List<Category> categories = JsonConvert.DeserializeObject<List<Category>>(inputJson)
                .Where(c=> c.Name != null && c.Name.Length >= 3 && c.Name.Length <= 15)
                .ToList();

            context.Categories.AddRange(categories);
            context.SaveChanges();

            return $"Successfully imported {categories.Count}";
        }

        public static string ImportProducts(ProductShopContext context, string inputJson)
        {
            Product[] products = JsonConvert.DeserializeObject<Product[]>(inputJson)
                .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.Length >= 3)
                .ToArray();

            context.AddRange(products);
            context.SaveChanges();

            return $"Successfully imported {products.Length}";
        }

        public static string ImportUsers(ProductShopContext context, string inputJson)
        {
            User[] users = JsonConvert.DeserializeObject<User[]>(inputJson)
                .Where(u=> u.LastName!= null && u.LastName.Length >= 3)
                .ToArray();

            context.Users.AddRange(users);
            context.SaveChanges();

            return $"Successfully imported {users.Length}";
        }

        public static string GetProductsInRange(ProductShopContext context)
        {
            var productsInRange = context.Products
                .Where(p => p.Price >= 500 && p.Price <= 1000)
                .Select(p => new ProductDto
                {
                    Name = p.Name,
                    Price = p.Price,
                    Seller = $"{p.Seller.FirstName} {p.Seller.LastName}"
                })
                .OrderBy(p=>p.Price)
                .ToList();

            var json = JsonConvert.SerializeObject(productsInRange, Formatting.Indented);

            return json;
        }

        public static string GetSoldProducts(ProductShopContext context)
        {
            var filteredUsers = context.Users
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    SoldProducts = u.ProductsSold
                        .Where(ps => ps.Buyer != null)
                        .Select(ps => new
                        {
                            Name = ps.Name,
                            Price = ps.Price,
                            BuyerFirstName = ps.Buyer.FirstName,
                            BuyerLastName = ps.Buyer.LastName
                        })
                        .ToArray()
                })
                .ToArray();

            DefaultContractResolver contractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var json = JsonConvert.SerializeObject(
                filteredUsers, 
                new JsonSerializerSettings
                { 
                    Formatting = Formatting.Indented,
                    ContractResolver = contractResolver
                });

            return json;
        }

        public static string GetUsersWithProducts(ProductShopContext context)
        {
            var filteredUsers = context
                .Users
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .OrderByDescending(u => u.ProductsSold.Count(ps => ps.Buyer != null))
                .Select(u => new
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Age = u.Age,
                    SoldProducts = new
                    {
                        Count = u.ProductsSold
                            .Count(ps => ps.Buyer != null),
                        Products = u.ProductsSold
                            .Where(ps => ps.Buyer != null)
                                .Select(ps => new
                                {
                                    Name = ps.Name,
                                    Price = ps.Price
                                })
                    .ToArray()
                    }
                })
                .ToArray();

            var result = new
            { 
                UserCount = filteredUsers.Length,
                Users = filteredUsers
            };

            DefaultContractResolver contractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var json = JsonConvert.SerializeObject(
                result,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = contractResolver,
                    NullValueHandling =NullValueHandling.Ignore
                });

            return json;
        }
    }
}