using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QZBarberShopBooking.Application.DTO.Shared
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Private constructor to force factory methods
        protected ApiResponse() { }

        // Factory methods with clear names
        public static ApiResponse<T> Success(T data, string message = "Operation successful")
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Failure(string errorMessage, string message = "An error occurred")
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { errorMessage }
            };
        }

        public static ApiResponse<T> Failure(List<string> errors, string message = "Validation failed")
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }

        public static ApiResponse<T> NotFound(string resourceName, object id)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = $"{resourceName} with id {id} was not found",
                Errors = new List<string> { "Resource not found" }
            };
        }

        public static ApiResponse<T> Unauthorized(string message = "Authentication required")
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { "Unauthorized access" }
            };
        }

        public static ApiResponse<T> Forbidden(string message = "Access denied")
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { "Insufficient permissions" }
            };
        }
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static new ApiResponse Success(string message = "Operation successful")
        {
            return new ApiResponse
            {
                IsSuccess = true,
                Message = message
            };
        }

        public static new ApiResponse Failure(string errorMessage, string message = "An error occurred")
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Message = message,
                Errors = new List<string> { errorMessage }
            };
        }

        public static new ApiResponse Failure(List<string> errors, string message = "Validation failed")
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }
    }
}