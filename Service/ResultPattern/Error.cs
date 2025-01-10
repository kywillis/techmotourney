namespace TecmoTourney.ResultPattern
{
    public class Error
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error code
        /// </summary>
        public string? Code { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">Code identifying the error.</param>
        public Error(string message, string? code = null)
        {
            Message = message;
            Code = code;
        }
    }
}
