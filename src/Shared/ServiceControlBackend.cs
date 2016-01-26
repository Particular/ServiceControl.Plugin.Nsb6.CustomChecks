namespace ServiceControl.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;
    using ServiceControl.Plugin.CustomChecks.Internal;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class ServiceControlBackend
    {
        public ServiceControlBackend(IDispatchMessages messageSender, ReadOnlySettings settings, CriticalError criticalError)
        {
            this.settings = settings;
            this.criticalError = criticalError;
            this.messageSender = messageSender;
            serializer = new JsonMessageSerializer(new SimpleMessageMapper());

            serviceControlBackendAddress = GetServiceControlAddress();

            circuitBreaker =
                new RepeatedFailuresOverTimeCircuitBreaker("ServiceControlConnectivity", TimeSpan.FromMinutes(2),
                    ex =>
                        criticalError.Raise(
                            "This endpoint is repeatedly unable to contact the ServiceControl backend to report endpoint information. You have the ServiceControl plugins installed in your endpoint. However, please ensure that the Particular ServiceControl service is installed on this machine, " +
                            "or if running ServiceControl on a different machine, then ensure that your endpoint's app.config / web.config, AppSettings has the following key set appropriately: ServiceControl/Queue. \r\n" +
                            @"For example: <add key=""ServiceControl/Queue"" value=""particular.servicecontrol@machine""/>" +
                            "\r\n", ex));
        }

        public async Task Send(ReportCustomCheckResult result, TimeSpan timeToBeReceived)
        {
            result.Apply(settings);

            byte[] body;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new[] { result }, stream);
                body = stream.ToArray();
            }

            //hack to remove the type info from the json
            var bodyString = Encoding.UTF8.GetString(body);

            var toReplace = ", " + result.GetType().Assembly.GetName().Name;

            bodyString = bodyString.Replace(toReplace, ", ServiceControl");

            body = Encoding.UTF8.GetBytes(bodyString);
            // end hack
            var headers = new Dictionary<string, string>();
            headers[Headers.EnclosedMessageTypes] = result.GetType().FullName;
            headers[Headers.ContentType] = ContentTypes.Json; //Needed for ActiveMQ transport
            headers[Headers.ReplyToAddress] = settings.LocalAddress();

            try
            {
                var outgoingMessage = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
                DiscardIfNotReceivedBefore[] constraints =
                {
                    new DiscardIfNotReceivedBefore(timeToBeReceived)
                };
                var unicastTransportOperation = new UnicastTransportOperation(outgoingMessage, serviceControlBackendAddress, constraints);
                var multicastOperations = Enumerable.Empty<MulticastTransportOperation>();
                UnicastTransportOperation[] unicastOperations = { unicastTransportOperation };

                await messageSender.Dispatch(new TransportOperations(multicastOperations, unicastOperations), new ContextBag()).ConfigureAwait(false);
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
            var queueName = ConfigurationManager.AppSettings[@"ServiceControl/Queue"];
            if (!string.IsNullOrEmpty(queueName))
            {
                return queueName;
            }

            // TODO: Is this right??
            string errorAddress;
            if (TryGetErrorQueueAddress(out errorAddress))
            {
                var qm = Parse(errorAddress);
                return "Particular.ServiceControl"+ "@" + qm.Item2;
            }

            if (VersionChecker.CoreVersionIsAtLeast(4, 1))
            {
                //audit config was added in 4.1
                string address;
                if (TryGetAuditAddress(out address))
                {
                    var qm = Parse(errorAddress);
                    return "Particular.ServiceControl" + "@" + qm.Item2;
                }
            }

            return null;
        }


        bool TryGetErrorQueueAddress(out string address)
        {
            var faultsForwarderConfig = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (!string.IsNullOrEmpty(faultsForwarderConfig?.ErrorQueue))
            {
                address = faultsForwarderConfig.ErrorQueue;
                return true;
            }
            address = null;
            return false;
        }

        bool TryGetAuditAddress(out string address)
        {
            var auditConfig = settings.GetConfigSection<AuditConfig>();
            if (!string.IsNullOrEmpty(auditConfig?.QueueName))
            {
                address = auditConfig.QueueName;
                return true;
            }
            address = null;

            return false;
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
                var operation = new UnicastTransportOperation(outgoingMessage, serviceControlBackendAddress);
                await messageSender.Dispatch(new TransportOperations(Enumerable.Empty<MulticastTransportOperation>(), new[]
                {
                    operation
                }), new ContextBag()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                const string errMsg = "This endpoint is unable to contact the ServiceControl Backend to report endpoint information. You have the ServiceControl plugins installed in your endpoint. However, please ensure that the Particular ServiceControl service is installed on this machine, " +
                                      "or if running ServiceControl on a different machine, then ensure that your endpoint's app.config / web.config, AppSettings has the following key set appropriately: ServiceControl/Queue. \r\n" +
                                      @"For example: <add key=""ServiceControl/Queue"" value=""particular.servicecontrol@machine""/>" +
                                      "\r\n Additional details: {0}";
                criticalError.Raise(errMsg, ex);
            }
        }

        static Tuple<string, string> Parse(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException("Invalid destination address specified", nameof(destination));
            }

            var arr = destination.Split('@');

            var queue = arr[0];
            var machine = RuntimeEnvironment.MachineName;

            if (string.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentException("Invalid destination address specified", nameof(destination));
            }

            if (arr.Length == 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost" && arr[1] != IPAddress.Loopback.ToString())
                    machine = arr[1];

            return new Tuple<string, string>(queue, machine);
        }

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        CriticalError criticalError;
        IDispatchMessages messageSender;

        JsonMessageSerializer serializer;
        string serviceControlBackendAddress;
        ReadOnlySettings settings;
    }
}