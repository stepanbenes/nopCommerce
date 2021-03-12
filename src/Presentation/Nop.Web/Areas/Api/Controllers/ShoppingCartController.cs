using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Factories;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Areas.Api.Controllers
{
    public record AddProductToCartModel(int ProductId, int? Quantity);

    [Area("Api")]
    [Route("api/shopping-cart")]
    public class ShoppingCartController : Controller
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public ShoppingCartController(
            IProductService productService,
            IShoppingCartService shoppingCartService,
            ICurrencyService currencyService,
            IStoreContext storeContext,
            IWorkContext workContext,
            IPermissionService permissionService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _storeContext = storeContext;
            _workContext = workContext;
            _permissionService = permissionService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Action methods

        [HttpPost]
        public async Task<IActionResult> AddProductToCart([FromBody] AddProductToCartModel model)
        {
            ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart;

            var product = await _productService.GetProductByIdAsync(model.ProductId);
            if (product is null)
            {
                return NotFound();
            }

            //we can add only simple products
            if (product.ProductType != ProductType.SimpleProduct)
            {
                return UnprocessableEntity("Only simple products could be added to the cart");
            }

            //update existing shopping cart item
            var updatecartitemid = 0;

            ShoppingCartItem updateCartItem = null;
            if (_shoppingCartSettings.AllowCartItemEditing && updatecartitemid > 0)
            {
                //search with the same cart type as specified
                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), shoppingCartType, (await _storeContext.GetCurrentStoreAsync()).Id);

                updateCartItem = cart.FirstOrDefault(x => x.Id == updatecartitemid);
                //not found? let's ignore it. in this case we'll add a new item
                //if (updatecartitem == null)
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "No shopping cart item found to update"
                //    });
                //}
                //is it this product?
                if (updateCartItem != null && product.Id != updateCartItem.ProductId)
                {
                    return UnprocessableEntity("This product does not match a passed shopping cart item identifier");
                }
            }

            var addToCartWarnings = new List<string>();

            //customer entered price
            decimal customerEnteredPrice = 0.0m;
            decimal customerEnteredPriceConverted = customerEnteredPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrencyAsync(customerEnteredPrice, await _workContext.GetWorkingCurrencyAsync());

            //entered quantity
            int quantity = model.Quantity ?? 1;

            //product and gift card attributes
            string attributes = null;

            //rental attributes
            DateTime? rentalStartDate = null;
            DateTime? rentalEndDate = null;

            var cartType = updateCartItem == null ? shoppingCartType :
                //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                updateCartItem.ShoppingCartType;

            await SaveItemAsync(updateCartItem, addToCartWarnings, product, cartType, attributes, customerEnteredPriceConverted, rentalStartDate, rentalEndDate, quantity);

            if (addToCartWarnings.Any())
            {
                return UnprocessableEntity(addToCartWarnings);
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return Unauthorized();

            var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);
            var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), cart);
            return Ok(model);
        }

        #endregion

        #region Helper methods

        private async Task SaveItemAsync(ShoppingCartItem updateCartItem, List<string> addToCartWarnings, Product product,
                            ShoppingCartType cartType, string attributes, decimal customerEnteredPriceConverted, DateTime? rentalStartDate,
                            DateTime? rentalEndDate, int quantity)
        {
            if (updateCartItem == null)
            {
                //add to the cart
                addToCartWarnings.AddRange(await _shoppingCartService.AddToCartAsync(await _workContext.GetCurrentCustomerAsync(),
                    product, cartType, (await _storeContext.GetCurrentStoreAsync()).Id,
                    attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true));
            }
            else
            {
                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), updateCartItem.ShoppingCartType, (await _storeContext.GetCurrentStoreAsync()).Id);

                var otherCartItemWithSameParameters = await _shoppingCartService.FindShoppingCartItemInTheCartAsync(
                    cart, updateCartItem.ShoppingCartType, product, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate);
                if (otherCartItemWithSameParameters != null &&
                    otherCartItemWithSameParameters.Id == updateCartItem.Id)
                {
                    //ensure it's some other shopping cart item
                    otherCartItemWithSameParameters = null;
                }
                
                //update existing item
                addToCartWarnings.AddRange(await _shoppingCartService.UpdateShoppingCartItemAsync(await _workContext.GetCurrentCustomerAsync(),
                    updateCartItem.Id, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity + (otherCartItemWithSameParameters?.Quantity ?? 0), true));

                if (otherCartItemWithSameParameters != null && !addToCartWarnings.Any())
                {
                    //delete the same shopping cart item (the other one)
                    await _shoppingCartService.DeleteShoppingCartItemAsync(otherCartItemWithSameParameters);
                }
            }
        }

        #endregion
    }
}
