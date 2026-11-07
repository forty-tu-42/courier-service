using Courier.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Courier.API.Auth;

public static class ApiErrors
{
    public static ObjectResult From(Exception ex)
    {
        var status = ex switch
        {
            InvalidOperationException => StatusCodes.Status400BadRequest,
            SqlException sql when sql.Number is 2627 or 2601 => StatusCodes.Status409Conflict,
            SqlException sql when sql.Number == 547 => StatusCodes.Status409Conflict,
            SqlException sql when sql.Number is 50001 or 50002 or 50003 or 50004
                or 50010 or 50011 or 50012 or 50013 or 50014 or 50020 or 50021 => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ApiError
        {
            Error = ex.Message,
            Code = status switch
            {
                409 => "CONFLICT",
                400 => "VALIDATION_ERROR",
                _ => "INTERNAL_ERROR"
            }
        })
        { StatusCode = status };
    }
}
