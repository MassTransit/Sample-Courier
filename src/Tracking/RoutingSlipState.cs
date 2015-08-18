namespace Tracking
{
    using System;
    using Automatonymous;


    public class RoutingSlipState :
        SagaStateMachineInstance
    {
        protected RoutingSlipState()
        {
        }

        public RoutingSlipState(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        /// <summary>
        /// The state of the saga
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// When the routing slip was started
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// When the routing slip was completed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The total duration of the routing slip
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// When the routing slip was created
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// The fault summary is an exception summary for the faulted routing slip
        /// </summary>
        public string FaultSummary { get; set; }

        /// <summary>
        /// This maps to the tracking number of the routing slip
        /// </summary>
        public Guid CorrelationId { get; set; }
    }
}