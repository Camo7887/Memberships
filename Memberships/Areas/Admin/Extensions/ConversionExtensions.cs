﻿using Memberships.Areas.Admin.Models;
using Memberships.Entities;
using Memberships.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;

namespace Memberships.Areas.Admin.Extensions
{
    public static class ConversionExtensions
    {
        // Converts IEnum<Products> into IEnum<ProductModel>
        public static async Task<IEnumerable<ProductModel>> Convert
            (this IEnumerable<Product> products, ApplicationDbContext db)
        {
            // If products table data is empty, return empty table list
            if (products.Count().Equals(0))
                return new List<ProductModel>();

            // We need the lists thats in the db
            var texts = await db.ProductLinkTexts.ToListAsync();
            var types = await db.ProductTypes.ToListAsync();

            // Link using linq
            return from p in products
                   select new ProductModel
                   {
                       Id = p.Id,
                       Title = p.Title,
                       Description = p.Description,
                       ImageUrl = p.ImageUrl,
                       ProductLinkTextId = p.ProductLinkTextId,
                       ProductTypeId = p.ProductTypeId,
                       ProductLinkTexts = texts,
                       ProductTypes = types
                   };
        }

        // Convert List ProductModel into 1 ProductModel
        // Copy Above class and remove IEnumerable
        public static async Task<ProductModel> Convert
            (this Product products, ApplicationDbContext db)
        {
            // We need the lists thats in the db
            var texts = await db.ProductLinkTexts.FirstOrDefaultAsync(
                p => p.Id.Equals(products.ProductLinkTextId));
            var types = await db.ProductTypes.FirstOrDefaultAsync(
                p => p.Id.Equals(products.ProductTypeId));

            // Link using linq
                   var model = new ProductModel
                   {
                       Id = products.Id,
                       Title = products.Title,
                       Description = products.Description,
                       ImageUrl = products.ImageUrl,
                       ProductLinkTextId = products.ProductLinkTextId,
                       ProductTypeId = products.ProductTypeId,
                       ProductLinkTexts = new List<ProductLinkText>(),
                       ProductTypes = new List<ProductType>()
                   };

            // Add data to model
            model.ProductLinkTexts.Add(texts);
            model.ProductTypes.Add(types);

            return model;
        }

        // For ProductItems ViewModel
        public static async Task<IEnumerable<ProductItemModel>> Convert
            (this IQueryable<ProductItem> productItems, ApplicationDbContext db)
        {
            // If products table data is empty, return empty table list
            if (productItems.Count().Equals(0))
                return new List<ProductItemModel>();

            // Link using linq
            return await (from pi in productItems
                          select new ProductItemModel
                          {
                              // We wont fill collections, because we dont need it in index view
                              ItemId = pi.ItemId,
                              ProductId = pi.ProductId,
                              ItemTitle = db.Items.FirstOrDefault(
                                  i => i.Id.Equals(pi.ItemId)).Title,
                              ProductTitle = db.Products.FirstOrDefault(
                                  p => p.Id.Equals(pi.ProductId)).Title
                          }).ToListAsync();
        }

        // Take one ProductItem and Convert into one ProductItemModel
        public static async Task<ProductItemModel> Convert
            (this ProductItem productItem, ApplicationDbContext db)
        {
            // Link using linq
            var model = new ProductItemModel
            {
                ProductId = productItem.ProductId,
                ItemId = productItem.ItemId,
                Products = await db.Products.ToListAsync(),
                Items = await db.Items.ToListAsync()
            };

            return model;
        }

        public static async Task<bool> CanChange
            (this ProductItem productItem, ApplicationDbContext db)
        {
            var oldPI = await db.ProductItems.CountAsync(pi =>
              pi.ProductId.Equals(productItem.OldProductId) &&
              pi.ItemId.Equals(productItem.OldItemId));

            var newPI = await db.ProductItems.CountAsync(pi =>
                pi.ProductId.Equals(productItem.OldProductId) &&
                pi.ItemId.Equals(productItem.OldItemId));

            return oldPI.Equals(1) && newPI.Equals(0);
        }

        public static async Task Change
            (this ProductItem productItem, ApplicationDbContext db)
        {
            var oldProductItem = await db.ProductItems.FirstOrDefaultAsync(
                    pi => pi.ProductId.Equals(productItem.OldProductId) &&
                    pi.ItemId.Equals(productItem.OldItemId));

            var newProductItem = await db.ProductItems.FirstOrDefaultAsync(
                pi => pi.ProductId.Equals(productItem.ProductId) &&
                pi.ItemId.Equals(productItem.ItemId));

            if (oldProductItem != null && newProductItem == null)
            {
                newProductItem = new ProductItem
                {
                    ItemId = productItem.ItemId,
                    ProductId = productItem.ProductId
                };

                using (var transaction = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        db.ProductItems.Remove(oldProductItem);
                        db.ProductItems.Add(newProductItem);

                        await db.SaveChangesAsync();
                        transaction.Complete();
                    }
                    catch { transaction.Dispose(); }
                }
            }
    }
}