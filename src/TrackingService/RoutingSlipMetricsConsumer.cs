namespace TrackingService
{
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Courier.Contracts;


    public class RoutingSlipMetricsConsumer :
        IConsumer<RoutingSlipCompleted>
    {
        readonly RoutingSlipMetrics _metrics;

        public RoutingSlipMetricsConsumer(RoutingSlipMetrics metrics)
        {
            _metrics = metrics;
        }

        public async Task Consume(ConsumeContext<RoutingSlipCompleted> context)
        {
            _metrics.AddComplete(context.Message.Duration);
        }
    }
}