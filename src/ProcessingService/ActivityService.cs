namespace ProcessingService
{
    using System;
    using System.Configuration;
    using System.Threading;
    using CourierSample;
    using MassTransit;
    using MassTransit.AzureServiceBusTransport;
    using MassTransit.Courier;
    using MassTransit.Courier.Factories;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Processing.Activities.Retrieve;
    using Processing.Activities.Validate;
    using Topshelf;
    using Topshelf.Logging;


    class ActivityService :
        ServiceControl
    {
        readonly LogWriter _log = HostLogger.Get<ActivityService>();

        IBusControl _busControl;
        BusHandle _busHandle;

        public bool Start(HostControl hostControl)
        {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            Console.WriteLine("Min: {0}", workerThreads);

            ThreadPool.SetMinThreads(200, completionPortThreads);

            _log.Info("Creating bus...");

            _busControl = Bus.Factory.CreateUsingAzureServiceBus(x =>
            {
                var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb",
                    ConfigurationManager.AppSettings["ServiceBusNamespace"], "ActivityService");

                IServiceBusHost host = x.Host(serviceUri, h =>
                {
                    ServiceBusTokenProviderSettings settings = new ServiceBusAccountSettings();

                    h.SharedAccessSignature(s =>
                    {
                        s.KeyName = settings.KeyName;
                        s.SharedAccessKey = settings.SharedAccessKey;
                        s.TokenTimeToLive = settings.TokenTimeToLive;
                        s.TokenScope = settings.TokenScope;
                    });
                });

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["ValidateActivityQueue"], e =>
                {
                    e.PrefetchCount = 100;
                    e.ExecuteActivityHost<ValidateActivity, ValidateArguments>(
                        DefaultConstructorExecuteActivityFactory<ValidateActivity, ValidateArguments>.ExecuteFactory);
                });

                string compQueue = ConfigurationManager.AppSettings["CompensateRetrieveActivityQueue"];

                Uri compAddress = host.Settings.GetInputAddress(new QueueDescription(compQueue));

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

            _busHandle = _busControl.Start();

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _log.Info("Stopping bus...");

            if (_busHandle != null)
                _busHandle.Stop(TimeSpan.FromSeconds(30));

            return true;
        }
    }
}