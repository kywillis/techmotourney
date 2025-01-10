using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace TecmoTourney.ResultPattern
{
    public static class ApiErrorExtension
    {
        /// <summary>
        /// Converts to actionresult.
        /// </summary>
        /// <typeparam name="TSuccess">The type of the success.</typeparam>
        /// <typeparam name="TFailure">The type of the failure.</typeparam>
        /// <param name="operation">The API result.</param>
        /// <returns></returns>
        public static IActionResult ToActionResult<TSuccess, TFailure>(this Operation<TSuccess, TFailure> operation) where TFailure : ApiError
        {
            if (operation.IsSuccess)
            {                
                if(operation.Data is bool booleanValue)
                {
                    if(booleanValue == true)
                        return new NoContentResult();                
                    else 
                        throw new Exception("return type was bool but value was false, this cannot be translated into a HTTP Code");
                }
                else 
                    return new OkObjectResult(operation.Data);
            }

            switch (operation.Failure!.HttpStatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new NotFoundObjectResult(new ErrorContent(operation.Failure.Message, operation.Failure.Code));
                case HttpStatusCode.Unauthorized:
                    return new UnauthorizedObjectResult(new ErrorContent(operation.Failure.Message, operation.Failure.Code));
                case HttpStatusCode.BadRequest:
                    return new BadRequestObjectResult(new ErrorContent(operation.Failure.Message, operation.Failure.Code));
                default:
                    return new ObjectResult(new ErrorContent(operation.Failure.Message, operation.Failure.Code))
                    {
                        StatusCode = (int)operation.Failure.HttpStatusCode,
                    };
            }
        }
    }

    public record ErrorContent(string ErrorMessage, string? ErrorCode = null);
}
