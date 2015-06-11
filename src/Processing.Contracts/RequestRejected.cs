namespace Processing.Contracts
{
    using System;

    /// <summary>
    /// Published when a request is rejected
    /// </summary>
    public interface RequestRejected
    {
        /// <summary>
        /// The requestId
        /// </summary>
        Guid RequestId { get; }

        /// <summary>
        /// The tracking number of the routing slip
        /// </summary>
        Guid TrackingNumber { get; }

        /// <summary>
        /// When the event was produced
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The code assigned to the rejection
        /// </summary>
        int ReasonCode { get; }

        /// <summary>
        /// A displayable version of the rejection reason
        /// </summary>
        string ReasonText { get; }
    }
}