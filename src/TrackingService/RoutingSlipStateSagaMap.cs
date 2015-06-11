namespace TrackingService
{
    using MassTransit.NHibernateIntegration;
    using Tracking;


    public class RoutingSlipStateSagaMap :
        SagaClassMapping<RoutingSlipState>
    {
        public RoutingSlipStateSagaMap()
        {
            Property(x => x.State);

            Property(x => x.CreateTime);
            Property(x => x.StartTime);
            Property(x => x.EndTime);
            Property(x => x.Duration);
        }
    }
}