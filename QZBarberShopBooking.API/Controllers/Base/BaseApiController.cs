using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Filters;
using QZBarberShopBooking.Application.DTO.Shared;

namespace QZBarberShopBooking.API.Controllers.Base
{
    [ApiController]
    [ServiceFilter(typeof(ValidationFilter))]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult HandleResult<T>(ApiResponse<T> result)
        {
            if (result == null)
                return NotFound(ApiResponse<T>.Failure("Resource not found", "Not found"));

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        protected IActionResult HandlePaginatedResult<T>(PaginatedResponse<T> result)
        {
            if (result == null || result.Data == null || !result.Data.Any())
                return NotFound(ApiResponse<PaginatedResponse<T>>.Failure("No data found", "Not found"));

            if (!result.IsSuccess)
                return BadRequest(result);

            // Add pagination headers
            Response.Headers.Append("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    result.PageNumber,
                    result.PageSize,
                    result.TotalPages,
                    result.TotalCount,
                    result.HasPreviousPage,
                    result.HasNextPage
                }));

            return Ok(result);
        }

        protected IActionResult HandleValidationErrors(List<string> errors)
        {
            var response = ApiResponse.Failure(errors, "Validation failed");
            return BadRequest(response);
        }

        protected IActionResult HandleNotFound(string resourceName, object id)
        {
            var response = ApiResponse.Failure($"{resourceName} with id {id} not found", "Resource not found");
            return NotFound(response);
        }

        protected IActionResult HandleUnauthorized(string message = "Authentication required")
        {
            var response = ApiResponse.Failure(message, "Unauthorized");
            return Unauthorized(response);
        }

        protected IActionResult HandleForbidden(string message = "Access denied")
        {
            var response = ApiResponse.Failure(message, "Forbidden");
            return StatusCode(403, response);
        }
    }
}
