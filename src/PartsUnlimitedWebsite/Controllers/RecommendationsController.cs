﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using PartsUnlimited.Models;
using PartsUnlimited.Recommendations;
using PartsUnlimited.WebsiteConfiguration;
using System;
using System.Linq;
using System.Threading.Tasks;
using PartsUnlimited.Repository;

namespace PartsUnlimited.Controllers
{
    public class RecommendationsController : Controller
    {
        private readonly IPartsUnlimitedContext _db;
        private readonly IRecommendationEngine _recommendation;
        private readonly IProductRepository _productRepository;
        private readonly IWebsiteOptions _option;

        public RecommendationsController(IPartsUnlimitedContext context, 
            IRecommendationEngine recommendationEngine, IProductRepository productRepository, IWebsiteOptions websiteOptions)
        {
            _db = context;
            _recommendation = recommendationEngine;
            _productRepository = productRepository;
            _option = websiteOptions;
        }

        public async Task<IActionResult> GetRecommendations(string recommendationId)
        {
            if (!_option.ShowRecommendations)
            {
                return new EmptyResult();
            }

            var recommendedProductIds = await _recommendation.GetRecommendationsAsync(recommendationId);

            var products = await _productRepository.LoadProductsFromRecommendation(recommendedProductIds);

            var recommendedProducts = products
                .Where(p => p != null && p.RecommendationId != Convert.ToInt32(recommendationId))
                .ToList();

            return PartialView("_Recommendations", recommendedProducts);
        }
    }
}