using Application.DTOs;
using Application.Ports.Input;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, IStockService stockService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _stockService = stockService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductResponseDto>>> GetProducts(
        [FromQuery] ProductFilterDto filter,
        CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting products with filter: {@Filter}", filter);
            var result = await _productService.GetFilteredAsync(filter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
        {
            var product = await _productService.GetByIdAsync(id, cancellationToken);
            return Ok(product);
        }

        [HttpGet("{id:guid}/stock")]
        [ProducesResponseType(typeof(StockInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockInfoDto>> GetStock(
        Guid id,
        CancellationToken cancellationToken)
        {
            var stock = await _stockService.GetStockInfoAsync(id, cancellationToken);
            return Ok(stock);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ResponseCache(Duration = 3600)]
        public ActionResult<IEnumerable<string>> GetCategories(CancellationToken cancellationToken)
        {
            var categories =  _productService.GetCategoriesAsync(cancellationToken);
            return Ok(categories);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
        {
            var product = await _productService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
        {
            var product = await _productService.UpdateAsync(id, dto, cancellationToken);
            return Ok(product);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/reserve")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> ReserveStock(
        Guid id,
        [FromBody] ReserveStockRequest request,
        CancellationToken cancellationToken)
        {
            var result = await _stockService.TryReserveStockAsync(id, request.Quantity, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:guid}/add-stock")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AddStock(
        Guid id,
        [FromBody] AddStockRequest request,
        CancellationToken cancellationToken)
        {
            await _stockService.AddStockAsync(id, request.Quantity, cancellationToken);
            return NoContent();
        }

    }
}

public record ReserveStockRequest(int Quantity);
public record AddStockRequest(int Quantity);
