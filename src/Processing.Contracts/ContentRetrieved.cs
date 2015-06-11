namespace Processing.Contracts
{
    using System;


    public interface ContentRetrieved
    {
        /// <summary>
        /// When the content was retrieved
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The content address
        /// </summary>
        Uri Address { get; }

        /// <summary>
        /// The local URI for the content
        /// </summary>
        Uri LocalAddress { get; }

        /// <summary>
        /// The local path of the content
        /// </summary>
        string LocalPath { get; }

        /// <summary>
        /// The length of the content
        /// </summary>
        long Length { get; }

        /// <summary>
        /// The content type 
        /// </summary>
        string ContentType { get; }
    }
}