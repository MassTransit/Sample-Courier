namespace CourierSample
{
    using System;
    using MassTransit.AzureServiceBusTransport;
    using Microsoft.ServiceBus;


    public class ServiceBusAccountSettings :
        ServiceBusTokenProviderSettings
    {
        const string KeyName = "MassTransitBuild";
        const string SharedAccessKey = "xsvaZOKYkX8JI5N+spLCkI9iu102jLhWFJrf0LmNPMw=";
        readonly TokenScope _tokenScope;
        readonly TimeSpan _tokenTimeToLive;

        public ServiceBusAccountSettings()
        {
            _tokenTimeToLive = TimeSpan.FromDays(1);
            _tokenScope = TokenScope.Namespace;

            ServiceBusEnvironment.SystemConnectivity.Mode = ConnectivityMode.Http;
        }

        string ServiceBusTokenProviderSettings.KeyName
        {
            get { return KeyName; }
        }

        string ServiceBusTokenProviderSettings.SharedAccessKey
        {
            get { return SharedAccessKey; }
        }

        TimeSpan ServiceBusTokenProviderSettings.TokenTimeToLive
        {
            get { return _tokenTimeToLive; }
        }

        TokenScope ServiceBusTokenProviderSettings.TokenScope
        {
            get { return _tokenScope; }
        }
    }
}