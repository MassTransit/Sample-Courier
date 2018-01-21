using GreenPipes;

namespace ProcessingService
{
    using System;
    using System.Configuration;
    using System.Threading;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Factories;
    using MassTransit.RabbitMqTransport;
    using MassTransit.Util;
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

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["ValidateActivityQueue"], e =>
                {
                    e.PrefetchCount = 100;
                    e.ExecuteActivityHost(
                        DefaultConstructorExecuteActivityFactory<ValidateActivity, ValidateArguments>.ExecuteFactory, c => c.UseRetry(r => r.Immediate(5)));
                });

                string compQueue = ConfigurationManager.AppSettings["CompensateRetrieveActivityQueue"];

                Uri compAddress = new Uri(string.Concat(ConfigurationManager.AppSettings["RabbitMQHost"], compQueue));

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["RetrieveActivityQueue"], e =>
                {
                    e.PrefetchCount = 100;
                    e.ExecuteActivityHost<RetrieveActivity, RetrieveArguments>(compAddress,  c=> c.UseRetry(r => r.Immediate(5)));
                });

                x.ReceiveEndpoint(host, ConfigurationManager.AppSettings["CompensateRetrieveActivityQueue"],
                    e => e.CompensateActivityHost<RetrieveActivity, RetrieveLog>(c => c.UseRetry(r => r.Immediate(5))));
            });

            _log.Info("Starting bus...");

            TaskUtil.Await(() => _busControl.StartAsync());

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