namespace Tracking
{
    using System;
    using System.Linq;
    using Automatonymous;
    using MassTransit.Courier.Contracts;
    using Processing.Contracts;


    /// <summary>
    /// This state machine observes the events of a routing slip, and captures the state
    /// of the routing slip from execution to completion/failure.
    /// </summary>
    public class RoutingSlipStateMachine :
        MassTransitStateMachine<RoutingSlipState>
    {
        public RoutingSlipStateMachine()
        {
            InstanceState(x => x.State, Executing, Completed, Faulted, CompensationFailed);

            Event(() => SlipCreated, x => x.CorrelateById(context => context.Message.TrackingNumber));
            Event(() => SlipCompleted, x => x.CorrelateById(context => context.Message.TrackingNumber));
            Event(() => SlipFaulted, x => x.CorrelateById(context => context.Message.TrackingNumber));
            Event(() => SlipCompensationFailed, x => x.CorrelateById(context => context.Message.TrackingNumber));

            // Events can arrive out of order, so we want to make sure that all observed events can created
            // the state machine instance
            Initially(
                When(SlipCreated)
                    .Then(HandleRoutingSlipCreated)
                    .TransitionTo(Executing),
                When(SlipCompleted)
                    .Then(HandleRoutingSlipCompleted)
                    .TransitionTo(Completed),
                When(SlipFaulted)
                    .Then(HandleRoutingSlipFaulted)
                    .TransitionTo(Faulted),
                When(SlipCompensationFailed)
                    .TransitionTo(CompensationFailed));

            // during any state, we can handle any of the events, to transition or capture previously
            // missed data.
            DuringAny(
                When(SlipCreated)
                    .Then(context => context.Instance.CreateTime = context.Data.Timestamp),
                When(SlipCompleted)
                    .Then(HandleRoutingSlipCompleted)
                    .TransitionTo(Completed),
                When(SlipFaulted)
                    .Then(HandleRoutingSlipFaulted)
                    .TransitionTo(Faulted),
                When(SlipCompensationFailed)
                    .TransitionTo(CompensationFailed));
        }


        public State Executing { get; private set; }
        public State Completed { get; private set; }
        public State Faulted { get; private set; }
        public State CompensationFailed { get; private set; }

        public Event<RoutingSlipCreated> SlipCreated { get; private set; }
        public Event<RoutingSlipCompleted> SlipCompleted { get; private set; }
        public Event<RoutingSlipFaulted> SlipFaulted { get; private set; }
        public Event<RoutingSlipCompensationFailed> SlipCompensationFailed { get; private set; }

        static void HandleRoutingSlipCreated(BehaviorContext<RoutingSlipState, RoutingSlipCreated> context)
        {
            context.Instance.CreateTime = context.Data.Timestamp;
        }

        static void HandleRoutingSlipCompleted(BehaviorContext<RoutingSlipState, RoutingSlipCompleted> context)
        {
            context.Instance.EndTime = context.Data.Timestamp;
            context.Instance.Duration = context.Data.Duration;
        }

        static void HandleRoutingSlipFaulted(BehaviorContext<RoutingSlipState, RoutingSlipFaulted> context)
        {
            context.Instance.EndTime = context.Data.Timestamp;
            context.Instance.Duration = context.Data.Duration;

            string faultSummary = string.Join(", ",
                context.Data.ActivityExceptions.Select(x => string.Format("{0}: {1}", x.Name, x.ExceptionInfo.Message)));

            context.Instance.FaultSummary = faultSummary;
        }
    }
}