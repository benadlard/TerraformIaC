﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Primitives;
using PartsUnlimited.Models;
using PartsUnlimited.Telemetry;
using PartsUnlimited.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartsUnlimited.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IPartsUnlimitedContext _db;
        private readonly ITelemetryProvider _telemetry;
        private readonly IAntiforgery _antiforgery;
        private readonly IProductLoader _productLoader;

        public ShoppingCartController(IPartsUnlimitedContext context, ITelemetryProvider telemetryProvider, 
            IAntiforgery antiforgery, IProductLoader productLoader)
        {
            _db = context;
            _telemetry = telemetryProvider;
            _antiforgery = antiforgery;
            _productLoader = productLoader;
        }

        //
        // GET: /ShoppingCart/

        public async Task<IActionResult> Index()
        {
            var cart = ShoppingCart.GetCart(_db, HttpContext, _productLoader);

            var items = await cart.GetCartItems();
            var itemsCount = items.Sum(x => x.Count);
            var subTotal = items.Sum(x => x.Count * x.Product.Price);
            var shipping = itemsCount * (decimal)5.00;
            var tax = (subTotal + shipping) * (decimal)0.05;
            var total = subTotal + shipping + tax;

            var costSummary = new OrderCostSummary
            {
                CartSubTotal = subTotal.ToString("C"),
                CartShipping = shipping.ToString("C"),
                CartTax = tax.ToString("C"),
                CartTotal = total.ToString("C")
            };


            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = items,
                CartCount = itemsCount,
                OrderCostSummary = costSummary
            };

            // Track cart review event with measurements
            _telemetry.TrackTrace("Cart/Server/Index");

            // Return the view
            return View(viewModel);
        }

        //
        // GET: /ShoppingCart/AddToCart/5

        public async Task<IActionResult> AddToCart(int id)
        {
            // Start timer for save process telemetry
            var startTime = System.DateTime.Now;

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(_db, HttpContext, _productLoader);

            cart.AddToCart(id);

            await _db.SaveChangesAsync(HttpContext.RequestAborted);

            // Trace add process
            var measurements = new Dictionary<string, double>()
            {
                {"ElapsedMilliseconds", System.DateTime.Now.Subtract(startTime).TotalMilliseconds }
            };
            _telemetry.TrackEvent("Cart/Server/Add", null, measurements);

            // Go back to the main store page for more shopping
            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var cookieToken = string.Empty;
            var formToken = string.Empty;
            StringValues tokenHeaders;
            string[] tokens = null;

            if (HttpContext.Request.Headers.TryGetValue("RequestVerificationToken", out tokenHeaders))
            {
                tokens = tokenHeaders.First().Split(':');
                if (tokens != null && tokens.Length == 2)
                {
                    cookieToken = tokens[0];
                    formToken = tokens[1];
                }
            }


            _antiforgery.ValidateTokens(HttpContext, new AntiforgeryTokenSet(formToken, cookieToken));

            // Start timer for save process telemetry
            var startTime = System.DateTime.Now;

            // Retrieve the current user's shopping cart
            var cart = ShoppingCart.GetCart(_db, HttpContext, _productLoader);

            // Get the name of the product to display confirmation
            // TODO [EF] Turn into one query once query of related data is enabled
            int productId = _db.CartItems.Single(item => item.CartItemId == id).ProductId;
            IProduct product = await _productLoader.Load(productId);

            // Remove from cart
            int itemCount = cart.RemoveFromCart(id);

            await _db.SaveChangesAsync(HttpContext.RequestAborted);

            string removed = (itemCount > 0) ? " 1 copy of " : string.Empty;

            // Trace remove process
            var measurements = new Dictionary<string, double>()
            {
                {"ElapsedMilliseconds", System.DateTime.Now.Subtract(startTime).TotalMilliseconds }
            };
            _telemetry.TrackEvent("Cart/Server/Remove", null, measurements);

            // Display the confirmation message
            var items = await cart.GetCartItems();
            var itemsCount = items.Sum(x => x.Count);
            var subTotal = items.Sum(x => x.Count * x.Product.Price);
            var shipping = itemsCount * (decimal)5.00;
            var tax = (subTotal + shipping) * (decimal)0.05;
            var total = subTotal + shipping + tax;

            var results = new ShoppingCartRemoveViewModel
            {
                Message = removed + product.Title +
                    " has been removed from your shopping cart.",
                CartSubTotal = subTotal.ToString("C"),
                CartShipping = shipping.ToString("C"),
                CartTax = tax.ToString("C"),
                CartTotal = total.ToString("C"),
                CartCount = itemsCount,
                ItemCount = itemCount,
                DeleteId = id
            };

            return Json(results);
        }
    }
}