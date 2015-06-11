namespace Processing.Activities.Retrieve
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Logging;


    public class RetrieveActivity :
        Activity<RetrieveArguments, RetrieveLog>
    {
        static readonly ILog _log = Logger.Get<RetrieveActivity>();

        public async Task<ExecutionResult> Execute(ExecuteContext<RetrieveArguments> context)
        {
            var sourceAddress = context.Arguments.Address;

            _log.DebugFormat("Retrieve Content: {0}", sourceAddress);

            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(sourceAddress);
                    if (response.IsSuccessStatusCode)
                    {
                        var localFileName = GetLocalFileName(response, sourceAddress);

                        _log.DebugFormat("Success, copying to local file: {0}", localFileName);

                        using (FileStream stream = File.Create(localFileName, 4096, FileOptions.Asynchronous))
                        {
                            await response.Content.CopyToAsync(stream);
                            stream.Close();

                            var fileInfo = new FileInfo(localFileName);
                            var localAddress = new Uri(fileInfo.FullName);

                            _log.DebugFormat("Completed, length = {0}", fileInfo.Length);

                            await context.Publish<ContentRetrieved>(new
                            {
                                Timestamp = DateTime.UtcNow,
                                Address = sourceAddress,
                                LocalPath = fileInfo.FullName,
                                LocalAddress = localAddress,
                                Length = fileInfo.Length,
                                ContentType = response.Content.Headers.ContentType.ToString(),
                            });

                            return context.Completed<RetrieveLog>(new Log(fileInfo.FullName));
                        }
                    }

                    string message = string.Format("Server returned a response status code: {0} ({1})",
                        (int)response.StatusCode, response.StatusCode);

                    _log.ErrorFormat("Failed to retrieve image: {0}", message);
                    await context.Publish<ContentNotFound>(new
                    {
                        Timestamp = DateTime.UtcNow,
                        Address = sourceAddress,
                        Reason = message,
                    });

                    return context.Completed();
                }
            }
            catch (HttpRequestException exception)
            {
                _log.Error("Exception from HttpClient", exception.InnerException);

                throw;
            }
        }

        static string GetLocalFileName(HttpResponseMessage response, Uri sourceAddress)
        {
            string localFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "content");

            Directory.CreateDirectory(localFilePath);

            string localFileName = Path.GetFullPath(Path.Combine(localFilePath, NewId.NextGuid().ToString()));
            Uri contentLocation = response.Content.Headers.ContentLocation ?? sourceAddress;
            if (response.Content.Headers.ContentDisposition != null &&
                Path.HasExtension(response.Content.Headers.ContentDisposition.FileName))
                localFileName += Path.GetExtension(response.Content.Headers.ContentDisposition.FileName);
            else if (Path.HasExtension(contentLocation.AbsoluteUri))
                localFileName += Path.GetExtension(contentLocation.AbsoluteUri);
            return localFileName;
        }

        public async Task<CompensationResult> Compensate(CompensateContext<RetrieveLog> context)
        {
            if (_log.IsErrorEnabled)
                _log.ErrorFormat("Removing local file: {0}", context.Log.LocalFilePath);

            return context.Compensated();
        }


        class Log :
            RetrieveLog
        {
            public Log(string localFilePath)
            {
                LocalFilePath = localFilePath;
            }

            public string LocalFilePath { get; private set; }
        }
    }
}