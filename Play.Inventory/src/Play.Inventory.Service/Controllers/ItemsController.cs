using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _itemsRepository;
        private readonly CatalogClient _catalogClient;

        public ItemsController(IRepository<InventoryItem> itemsRepository, CatalogClient catalogClient)
        {
            _itemsRepository = itemsRepository;
            _catalogClient = catalogClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var catalogItems = await _catalogClient.GetCatalogItemsAsync();
            var inventoryItemEntities = await _itemsRepository.GetAllAsync(item => item.UserId == userId);

            var invetoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(invetoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemDto grantItemDto)
        {
            var inventoryItem = await _itemsRepository.GetAsync(
                item => item.UserId == grantItemDto.UserId && item.CatalogItemId == grantItemDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemDto.CatalogItemId,
                    UserId = grantItemDto.UserId,
                    Quantity = grantItemDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemDto.Quantity;
                await _itemsRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }
    }
}