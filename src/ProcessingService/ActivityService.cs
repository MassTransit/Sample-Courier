namespace ProcessingService
{
    using System;
    using System.Configuration;
    using System.Threading;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Factories;
    using MassTransit.RabbitMqTransport;
    using Processing.Activities.Retrieve;
    using Processing.Activities.Validate;
    using Topshelf;
    using Topshelf.Logging;


    class ActivityService :
        ServiceControl
    {
        readonly LogWriter _log = HostLogger.Get<ActivityService>();

        IBusControl _busControl;

        public bool Start(HostControl hostControl)
        {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            Console.WriteLine("Min: {0}", workerThreads);

            ThreadPool.SetMinThreads(200, completionPortThreads);

            _log.Info("Creating bus...");

            _busControl = Bus.Factory.CreateUsingRabbitMq(x =>
            {
                IRabbitMqHost host = x.Host(new Uri(ConfigurationManager.AppSettings["RabbitMQHost"]), h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                x.EnabledPerformanceCounters();

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["ValidateActivityQueue"], e =>
                {
                    e.PrefetchCount = 100;
                    e.ExecuteActivityHost<ValidateActivity, ValidateArguments>(
                        DefaultConstructorExecuteActivityFactory<ValidateActivity, ValidateArguments>.ExecuteFactory);
                });

                string compQueue = ConfigurationManager.AppSettings["CompensateRetrieveActivityQueue"];

                Uri compAddress = host.Settings.GetQueueAddress(compQueue);

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["RetrieveActivityQueue"], e =>
                {
                    e.PrefetchCount = 100;
                    //                    e.Retry(Retry.Selected<HttpRequestException>().Interval(5, TimeSpan.FromSeconds(1)));
                    e.ExecuteActivityHost<RetrieveActivity, RetrieveArguments>(compAddress);
                });

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["CompensateRetrieveActivityQueue"],
                    e => e.CompensateActivityHost<RetrieveActivity, RetrieveLog>());
            });

            _log.Info("Starting bus...");

            _busControl.Start();

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _log.Info("Stopping bus...");

            _busControl?.Stop();

            return true;
        }
    }
}