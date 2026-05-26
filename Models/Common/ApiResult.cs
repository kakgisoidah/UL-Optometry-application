// ════════════════════════════════════════════════════════════════════════
//  Models/Common/ApiResult.cs
//  Generic result wrapper returned by every service method.
//  Avoids try/catch boilerplate in ViewModels.
// ════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UL_Optometry.Models.Common;

public class ApiResult<T>
{
    public T?     Data    { get; private init; }
    public string? Error  { get; private init; }
    public bool   Success { get; private init; }

    public static ApiResult<T> Ok(T data)      => new() { Data = data, Success = true };
    public static ApiResult<T> Fail(string err)=> new() { Error = err, Success = false };
}

/// <summary>Non-generic version for operations that return no data.</summary>
public class ApiResult
{
    public string? Error  { get; private init; }
    public bool   Success { get; private init; }

    public static ApiResult Ok()               => new() { Success = true };
    public static ApiResult Fail(string err)   => new() { Error = err, Success = false };
}
