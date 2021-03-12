using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/catalog")]
    public class CatalogController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly IPictureService _pictureService;

        public CatalogController(
            ICategoryService categoryService,
            ICatalogModelFactory catalogModelFactory,
            IPictureService pictureService)
        {
            _categoryService = categoryService;
            _catalogModelFactory = catalogModelFactory;
            _pictureService = pictureService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categoryModels = await _catalogModelFactory.PrepareHomepageCategoryModelsAsync();
            
            // TODO: log activity

            return Ok(categoryModels);
        }

        [HttpGet("categories/{categoryId}")]
        public async Task<IActionResult> GetCategory([FromRoute] int categoryId, [FromQuery] CatalogProductsCommand command)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!await CheckCategoryAvailabilityAsync(category))
                return NotFound();

            // TODO: log activity

            var model = await _catalogModelFactory.PrepareCategoryModelAsync(category, command);
            return Ok(model);
        }

        [HttpGet("categories/{categoryId}/products")]
        public async Task<IActionResult> GetCategoryProducts([FromRoute] int categoryId, [FromQuery] CatalogProductsCommand command)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (!await CheckCategoryAvailabilityAsync(category))
                return NotFound();

            var model = await _catalogModelFactory.PrepareCategoryProductsModelAsync(category, command);
            return Ok(model);
        }

        #region Helper methods

        private Task<bool> CheckCategoryAvailabilityAsync(Category category)
        {
            // TODO: check user permissions
            bool isAvailable = category is not null && category.Published;

            return Task.FromResult(isAvailable);
        }

        #endregion
    }
}
