namespace Client
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using log4net.Config;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Courier.Contracts;
    using MassTransit.Log4NetIntegration.Logging;
    using MassTransit.RabbitMqTransport;
    using Processing.Contracts;


    class Program
    {
        static IRabbitMqHost _host;

        static void Main()
        {
            ConfigureLogger();

            // MassTransit to use Log4Net
            Log4NetLogger.Use();


            IBusControl busControl = CreateBus();

            BusHandle busHandle = busControl.Start();

            string validateQueueName = ConfigurationManager.AppSettings["ValidateActivityQueue"];

            Uri validateAddress = _host.Settings.GetQueueAddress(validateQueueName);

            string retrieveQueueName = ConfigurationManager.AppSettings["RetrieveActivityQueue"];

            Uri retrieveAddress = _host.Settings.GetQueueAddress(retrieveQueueName);


            try
            {
                for (;;)
                {
                    Console.Write("Enter an address (quit exits): ");
                    string requestAddress = Console.ReadLine();
                    if (requestAddress == "quit")
                        break;

                    if (string.IsNullOrEmpty(requestAddress))
                        requestAddress = "http://www.microsoft.com/index.html";

                    int limit = 1;

                    if (requestAddress.All(x => char.IsDigit(x) || char.IsPunctuation(x)))
                    {
                        string[] values = requestAddress.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                        requestAddress = values[0];
                        if (values.Length > 1)
                        {
                            limit = int.Parse(values[1]);
                            Console.WriteLine("Sending {0}", limit);
                        }
                    }

                    switch (requestAddress)
                    {
                        case "0":
                            requestAddress = "http://www.microsoft.com/index.html";
                            break;
                        case "1":
                            requestAddress = "http://i.imgur.com/Iroma7d.png";
                            break;
                        case "2":
                            requestAddress = "http://i.imgur.com/NK8eZUe.jpg";
                            break;
                    }

                    Uri requestUri;
                    try
                    {
                        requestUri = new Uri(requestAddress);
                    }
                    catch (UriFormatException)
                    {
                        Console.WriteLine("The URL entered is invalid: " + requestAddress);
                        continue;
                    }

                    IEnumerable<Task> tasks = Enumerable.Range(0, limit).Select(x => Task.Run(async () =>
                    {
                        var builder = new RoutingSlipBuilder(NewId.NextGuid());

                        builder.AddActivity("Validate", validateAddress);
                        builder.AddActivity("Retrieve", retrieveAddress);

                        builder.SetVariables(new
                        {
                            RequestId = NewId.NextGuid(),
                            Address = requestUri,
                        });

                        RoutingSlip routingSlip = builder.Build();

                        await busControl.Publish<RoutingSlipCreated>(new
                        {
                            TrackingNumber = routingSlip.TrackingNumber,
                            Timestamp = routingSlip.CreateTimestamp,
                        });

                        await busControl.Execute(routingSlip);
                    }));

                    Task.WaitAll(tasks.ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception!!! OMG!!! {0}", ex);
                Console.ReadLine();
            }
            finally
            {
                busHandle.Stop(TimeSpan.FromSeconds(30));
            }
        }


        static IBusControl CreateBus()
        {
            return Bus.Factory.CreateUsingRabbitMq(x =>
            {
                _host = x.Host(new Uri(ConfigurationManager.AppSettings["RabbitMQHost"]), h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
            });
        }

        static void ConfigureLogger()
        {
            const string logConfig = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<log4net>
  <root>
    <level value=""DEBUG"" />
    <appender-ref ref=""console"" />
  </root>
  <appender name=""console"" type=""log4net.Appender.ColoredConsoleAppender"">
    <layout type=""log4net.Layout.PatternLayout"">
      <conversionPattern value=""%m%n"" />
    </layout>
  </appender>
</log4net>";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(logConfig)))
            {
                XmlConfigurator.Configure(stream);
            }
        }
    }
}