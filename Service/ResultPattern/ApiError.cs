using System.Net;

namespace TecmoTourney.ResultPattern
{
    /// <summary>
    /// Api Error
    /// </summary>
    public class ApiError : Error
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        /// <value>
        /// The HTTP status code.
        /// </value>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        public ApiError(string errorMessage, HttpStatusCode httpStatusCode) : base(errorMessage)
        {
            HttpStatusCode = httpStatusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorCode">Code identifying the error.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        public ApiError(string errorMessage, string errorCode, HttpStatusCode httpStatusCode) : base(errorMessage, errorCode)
        {
            HttpStatusCode = httpStatusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiError"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="httpStatusCode">The code.</param>
        public ApiError(Error error, HttpStatusCode httpStatusCode) : this(error.Message, error.Code!, httpStatusCode) { }
    }
}
