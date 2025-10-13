using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Helpers.BaseClasses
{
    public class BaseResponse
    {
        public int StatusCode { get; set; } 
        public string Message { get; set; } 
        public object? Data { get; set; } 
        public object? AdditionalData { get; set; } 
        public object? Errors { get; set; }
    }

    public class BaseResponse<T>
    {
        public int StatusCode { get; set; } 
        public string Message { get; set; } 
        public T? Data { get; set; } = default!;
        public object? AdditionalData { get; set; }
        public object? Errors { get; set; }
    }

    public class BaseResponse<TData, TAdditionalData>
    {
        public int StatusCode { get; set; } 
        public string Message { get; set; } 
        public TData? Data { get; set; }
        public TAdditionalData? AdditionalData { get; set; }
        public object? Errors { get; set; }
    }
}
