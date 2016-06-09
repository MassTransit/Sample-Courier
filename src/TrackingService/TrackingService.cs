namespace TrackingService
{
    using System;
    using System.Configuration;
    using Automatonymous;
    using CourierSample;
    using MassTransit;
    using MassTransit.AzureServiceBusTransport;
    using MassTransit.NHibernateIntegration.Saga;
    using MassTransit.Saga;
    using Microsoft.ServiceBus;
    using NHibernate;
    using Topshelf;
    using Topshelf.Logging;
    using Tracking;


    class TrackingService :
        ServiceControl
    {
        readonly LogWriter _log = HostLogger.Get<TrackingService>();
        RoutingSlipMetrics _activityMetrics;

        IBusControl _busControl;
        RoutingSlipStateMachine _machine;
        RoutingSlipMetrics _metrics;
        SQLiteSessionFactoryProvider _provider;
        ISagaRepository<RoutingSlipState> _repository;
        ISessionFactory _sessionFactory;

        public bool Start(HostControl hostControl)
        {
            _log.Info("Creating bus...");

            _metrics = new RoutingSlipMetrics("Routing Slip");
            _activityMetrics = new RoutingSlipMetrics("Validate Activity");

            _machine = new RoutingSlipStateMachine();
            _provider = new SQLiteSessionFactoryProvider(false, typeof(RoutingSlipStateSagaMap));
            _sessionFactory = _provider.GetSessionFactory();

            _repository = new NHibernateSagaRepository<RoutingSlipState>(_sessionFactory);

            _busControl = Bus.Factory.CreateUsingAzureServiceBus(x =>
            {
                var serviceUri = ServiceBusEnvironment.CreateServiceUri("sb",
                    ConfigurationManager.AppSettings["ServiceBusNamespace"], "TrackingService");

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

                x.EnablePerformanceCounters();

                x.ReceiveEndpoint(host, "routing_slip_metrics", e =>
                {
                    e.PrefetchCount = 100;
                    e.UseRetry(Retry.None);
                    e.Consumer(() => new RoutingSlipMetricsConsumer(_metrics));
                });

                x.ReceiveEndpoint(host, "routing_slip_activity_metrics", e =>
                {
                    e.PrefetchCount = 100;
                    e.UseRetry(Retry.None);
                    e.Consumer(() => new RoutingSlipActivityConsumer(_activityMetrics, "Validate"));
                });

                x.ReceiveEndpoint(host, "routing_slip_state", e =>
                {
                    e.PrefetchCount = 8;
                    e.UseConcurrencyLimit(1);
                    e.StateMachineSaga(_machine, _repository);
                });
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