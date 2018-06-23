﻿using System;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace ServiceFabric.Remoting.CustomHeaders.ReliableServices
{
    public class ExtendedServiceProxy
    {
        public static TServiceInterface Create<TServiceInterface>(Uri serviceUri, CustomHeaders customHeaders = null,
            ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null)
            where TServiceInterface : IService
        {
            return Create<TServiceInterface>(serviceUri, () => customHeaders, partitionKey, targetReplicaSelector, listenerName);
        }

        public static TServiceInterface Create<TServiceInterface>(Uri serviceUri, Func<CustomHeaders> customHeaderProvider,
            ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null)
            where TServiceInterface : IService
        {
            var methodNameProvider = new MethodNameProvider();

            var proxyFactory = new ServiceProxyFactory(handler =>
                new ExtendedServiceRemotingClientFactory(
                    new FabricTransportServiceRemotingClientFactory(remotingCallbackMessageHandler: handler), customHeaderProvider, methodNameProvider));
            var proxy = proxyFactory.CreateServiceProxy<TServiceInterface>(serviceUri, partitionKey, targetReplicaSelector, listenerName);
            methodNameProvider.AddMethodsForProxyOrService(proxy.GetType().GetInterfaces(), typeof(IService));
            return proxy;
        }
    }
}