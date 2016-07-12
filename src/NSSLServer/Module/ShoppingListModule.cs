﻿using Microsoft.AspNetCore.Mvc;
using NSSLServer.Models;
using NSSLServer.Models.DatabaseConnection;
using System.Linq;
using System.Threading.Tasks;
using static Shared.RequestClasses;
using static Shared.ResultClasses;

namespace NSSLServer.Features
{
    [Route("shoppinglists"),WithDbContext]
    public class ShoppingListModule : AuthenticatingController
    {
       
        [HttpGet, Route("products/{identifier}")]
        public async Task<IActionResult> GetProduct(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return new BadRequestResult();
            long i;
            if ((identifier.Length == 8 || identifier.Length == 13) && long.TryParse(identifier, out i))
                return Json((await ProductSourceManager.FindProductByCode(identifier)));
            else
                return Json((await ProductSourceManager.FindProductsByName(identifier)));
        }

        [HttpGet]
        [Route("{listId}")]
        public async Task<IActionResult> GetList(int listId)
        => listId != 0 ? (IActionResult)(Json(await ShoppingListManager.LoadShoppingList(listId, Session.Id))) : new BadRequestResult();

        [HttpPut]
        [Route("{listId}/contributors")]
        public async Task<IActionResult> UpdateOwner(int listId, [FromBody]TransferOwnershipArgs args)
        {
            if (listId == 0 || args.NewOwnerId.HasValue == false)
                return new BadRequestResult();
            return Json(await ShoppingListManager.TransferOwnership(Context, listId, Session.Id, args.NewOwnerId.Value));
        }

        [HttpDelete]
        [Route("{listId}/contributors/{contributorId}")]
        public async Task<IActionResult> DeleteContributor(int listId, int contributorId)
        {
            if (listId == 0 || contributorId == 0)
                return new BadRequestResult();

            return Json(await ShoppingListManager.DeleteContributor(Context, listId, Session.Id, contributorId));

        }

        [HttpPost, Route("{listId}/contributors")]
        public async Task<IActionResult> AddContributor(int listId, [FromBody]AddContributorArgs args)
        {
            if (listId == 0 || string.IsNullOrWhiteSpace(args.Name))
                return Json(new AddContributorResult { Error = "asdf" });// new Response { StatusCode = HttpStatusCode.BadRequest };

            User u = await UserManager.FindUserByName(Context.Connection, args.Name); ;

            if (u == null)
                return Json(new AddContributorResult { Error = "User not found" });

            var cont = await ShoppingListManager.AddContributor(Context, listId, Session.Id, u.Id);

            if (cont == null)
                return Json(new AddContributorResult { Success = false, Error = "Error" });
            return Json(new AddContributorResult { Success = true, Id = cont.Id, Name = u.Username });
        }


        [HttpPut, Route("{listId}/products/{productId}")]
        public async Task<IActionResult> ChangeProduct(int listId, int productId, [FromBody]ChangeProductArgs args)
        {
            if (listId == 0 || productId == 0 || !args.Change.HasValue)
                return new BadRequestResult();
            return Json((await ShoppingListManager.ChangeProduct(Context, listId, Session.Id, productId, args.Change.Value)));
        }




        [HttpPut, Route("{listId}")]
        public async Task<IActionResult> UpdateList(int listId, [FromBody]ChangeListNameArgs args)
        {
            if (listId == 0 || string.IsNullOrWhiteSpace(args.Name))
                return Json(new Result { Error = "Asdf" });
            await ShoppingListManager.ChangeListname(Context, listId, Session.Id, args.Name);
            return Json(new Result { Success = true });
        }

        [HttpDelete, Route("{listId}")]
        public async Task<IActionResult> DeleteList(int listId)
        {
            if (listId == 0)
                return Json(new Result { Error = "No list id was provided", Success = false });
            return Json((await ShoppingListManager.DeleteList(Context, listId, Session.Id)));
        }

        [HttpPost, Route("{listId}")]
        public async Task<IActionResult> AddList(int listId, [FromBody]AddListArgs args)
        => Json((await ShoppingListManager.AddList(Context, args.Name, Session.Id)));


        [HttpDelete, Route("{listId}/products/{productId}")]
        public async Task<IActionResult> DeleteProduct(int listId, int productId)
        {
            if (listId == 0 || productId == 0)
                return new BadRequestResult();
            return Json((await ShoppingListManager.DeleteProduct(Context, listId, Session.Id, productId)));
        }

        [HttpPost, Route("{listId}/products")]
        public async Task<IActionResult> AddProduct(int listId, [FromBody]AddProductArgs args)
        {
            if (listId == 0 || (args.Gtin == "" && args.ProductName == ""))
                return new BadRequestResult();
            return Json((await ShoppingListManager.AddProduct(Context, listId, Session.Id, args.ProductName, args.Gtin, args.Amount.Value)));
        }
    }
}