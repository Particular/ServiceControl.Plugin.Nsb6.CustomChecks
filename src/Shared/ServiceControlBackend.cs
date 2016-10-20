namespace ServiceControl.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using NServiceBus.Unicast.Transport;
    using ServiceControl.Plugin.CustomChecks;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class ServiceControlBackend
    {
        public ServiceControlBackend(IDispatchMessages messageSender, ReadOnlySettings settings, CriticalError criticalError)
        {
            this.settings = settings;
            this.criticalError = criticalError;
            this.messageSender = messageSender;
            serializer = new DataContractJsonSerializer(typeof(ReportCustomCheckResult), new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("o"),
                EmitTypeInformation = EmitTypeInformation.Always,
            });

            serviceControlBackendAddress = GetServiceControlAddress();

            circuitBreaker =
                new RepeatedFailuresOverTimeCircuitBreaker("ServiceControlConnectivity", TimeSpan.FromMinutes(2),
                    ex =>
                        criticalError.Raise("You have ServiceControl plugins installed in your endpoint, however, this endpoint is repeatedly unable to contact the ServiceControl backend to report endpoint information.", ex));
        }

        public async Task Send(ReportCustomCheckResult result, TimeSpan timeToBeReceived)
        {
            result.Apply(settings);

            byte[] body;
            using (var stream = new MemoryStream())
            {
                var resultAsObject = new object[] { result };
                serializer.WriteObject(stream, resultAsObject);
                body = stream.ToArray();
            }

            //hack to remove the type info from the json
            var bodyString = Encoding.UTF8.GetString(body);

            var toReplace = "\"__type\":\"ReportCustomCheckResult:#ServiceControl.Plugin.CustomChecks.Messages\"";

            bodyString = bodyString.Replace(toReplace, "\"$type\":\"ServiceControl.Plugin.CustomChecks.Messages.ReportCustomCheckResult, ServiceControl\"");

            body = Encoding.UTF8.GetBytes(bodyString);
            // end hack
            var headers = new Dictionary<string, string>();
            headers[Headers.EnclosedMessageTypes] = result.GetType().FullName;
            headers[Headers.ContentType] = ContentTypes.Json; //Needed for ActiveMQ transport
            headers[Headers.ReplyToAddress] = settings.LocalAddress();
            headers[Headers.MessageIntent] = sendIntent;

            try
            {
                var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
                var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(serviceControlBackendAddress), deliveryConstraints: new List<DeliveryConstraint> { new DiscardIfNotReceivedBefore(timeToBeReceived) });
                await messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag()).ConfigureAwait(false);
                circuitBreaker.Success();
            }
            catch (Exception ex)
            {
                await circuitBreaker.Failure(ex).ConfigureAwait(false);
            }
        }

        public Task Send(ReportCustomCheckResult messageToSend)
        {
            return Send(messageToSend, TimeSpan.MaxValue);
        }

        string GetServiceControlAddress()
        {
            var queueName = ConfigurationManager.AppSettings["ServiceControl/Queue"];

            if (!string.IsNullOrEmpty(queueName))
            {
                return queueName;
            }

            if (settings.HasSetting("ServiceControl.Queue"))
            {
                queueName = settings.Get<string>("ServiceControl.Queue");
            }

            if (!string.IsNullOrEmpty(queueName))
            {
                return queueName;
            }

            const string errMsg = @"You have ServiceControl plugins installed in your endpoint, however, the Particular ServiceControl queue is not specified.
Please ensure that the Particular ServiceControl queue is specified either via code (config.CustomChecksPlugin(servicecontrolQueue)) or AppSettings (eg. <add key=""ServiceControl/Queue"" value=""particular.servicecontrol@machine""/>).";

            throw new Exception(errMsg);
        }

        public async Task VerifyIfServiceControlQueueExists()
        {
            try
            {
                // In order to verify if the queue exists, we are sending a control message to SC.
                // If we are unable to send a message because the queue doesn't exist, then we can fail fast.
                // We currently don't have a way to check if Queue exists in a transport agnostic way,
                // hence the send.
                var outgoingMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);
                outgoingMessage.Headers[Headers.ReplyToAddress] = settings.LocalAddress();
                var operation = new TransportOperation(outgoingMessage, new UnicastAddressTag(serviceControlBackendAddress));
                await messageSender.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                const string errMsg = @"You have ServiceControl plugins installed in your endpoint, however, this endpoint is unable to contact the ServiceControl Backend to report endpoint information.
Please ensure that the Particular ServiceControl queue specified is correct.";

                criticalError.Raise(errMsg, ex);
            }
        }

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        CriticalError criticalError;
        IDispatchMessages messageSender;
        readonly string sendIntent = MessageIntentEnum.Send.ToString();
        DataContractJsonSerializer serializer;
        string serviceControlBackendAddress;
        ReadOnlySettings settings;
    }
}