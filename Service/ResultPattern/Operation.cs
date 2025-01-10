using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TecmoTourney.ResultPattern
{

    /// <summary>
    /// Notification pattern implementation.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success.</typeparam>
    /// <typeparam name="TFailure">The type of the failure.</typeparam>
    public struct Operation<TSuccess, TFailure> where TFailure : Error
    {
        /// <summary>
        /// Gets the success.
        /// </summary>
        /// <value>
        /// The success.
        /// </value>
        public TSuccess? Data { get; }

        /// <summary>
        /// Gets the failure.
        /// </summary>
        /// <value>
        /// The failure.
        /// </value>
        public TFailure? Failure { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is success.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is success; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccess { get; }

        private Operation(TSuccess success)
        {
            IsSuccess = true;
            Data = success;
            Failure = default;
        }

        private Operation(TFailure failure)
        {
            IsSuccess = false;
            Data = default;
            Failure = failure;
        }

        /// <summary>
        /// Creates the specified success.
        /// </summary>
        /// <param name="success">The success.</param>
        /// <returns></returns>
        public static implicit operator Operation<TSuccess, TFailure>(TSuccess success) => new(success);

        /// <summary>
        /// Creates the specified failure.
        /// </summary>
        /// <param name="failure"></param>
        public static implicit operator Operation<TSuccess, TFailure>(TFailure failure) => new(failure);
    }
}
