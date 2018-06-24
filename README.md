# ServiceFabric.Remoting.CustomHeaders

This package allows injecting custom headers into remoting messages (Actors and Reliable Services, V2 remoting only) at runtime. The headers are available client side to read. 
It also provides BeforeHandleRequestResponseAsync and AfterHandleRequestResponseAsync to act on remoting events.'

Common used classes:

- [ExtendedActorProxy](https://github.com/Expecho/ServiceFabric-Remoting-CustomHeaders/blob/master/src/ServiceFabric.Remoting.CustomHeaders/Actors/ExtendedActorProxy.cs)
- [ExtendedServiceProxy](https://github.com/Expecho/ServiceFabric-Remoting-CustomHeaders/blob/master/src/ServiceFabric.Remoting.CustomHeaders/ReliableServices/ExtendedServiceProxy.cs)
- [CustomHeaders](https://github.com/Expecho/ServiceFabric-Remoting-CustomHeaders/blob/master/src/ServiceFabric.Remoting.CustomHeaders/CustomHeaders.cs)
- [RemotingContext](https://github.com/Expecho/ServiceFabric-Remoting-CustomHeaders/blob/master/src/ServiceFabric.Remoting.CustomHeaders/RemotingContext.cs)

## Examples

This repository includes a Service Fabric application for demonstration purposes. A [Console Application](https://github.com/Expecho/ServiceFabric-Remoting-CustomHeaders/blob/master/src/Demo/Program.cs) is used to access the application and shows the usage of the package.

## Usage scenarios

Custom headers can be used to pass data between the sender and the receiver like tracing information or security context data. Using the BeforeHandleRequestResponseAsync and AfterHandleRequestResponseAsync actions additional logging can be applied monitor the flow between remoting calls.

## How to set up 

### For Reliable Services

Create a listener that can handle the requests

```csharp
protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
{
    yield return new ServiceInstanceListener(context =>
        new FabricTransportServiceRemotingListener(context,
            new ExtendedServiceRemotingMessageDispatcher(context, this)
            {
                // Optional, allows call interception. Executed before the response is handled
                BeforeHandleRequestResponseAsync = (message, method) =>
                {
                    ServiceEventSource.Current.ServiceMessage(Context, $"BeforeHandleRequestResponseAsync {method}");
                    return Task.CompletedTask;
                },
                // Optional, allows call interception. Executed after the response is handled
                AfterHandleRequestResponseAsync = (message, method) =>
                {
                    ServiceEventSource.Current.ServiceMessage(Context, $"AfterHandleRequestResponseAsync {method}");
                    return Task.CompletedTask;
                }
            }));
}
```        
### For Actors

Register the actor using the `ExtendedActorService` service:

```csharp
ActorRuntime.RegisterActorAsync<DemoActor> (
(context, actorType) =>
   {
	   var service = new ExtendedActorService(context, actorType)
	   {
		   // Optional, allows call interception. Executed before the response is handled
		   BeforeHandleRequestResponseAsync = (message, method, id) =>
		   {
			   ActorEventSource.Current.Message($"BeforeHandleRequestResponseAsync {method} for actor {id.ToString()}");
			   return Task.CompletedTask;
		   },
		   // Optional, allows call interception. Executed after the response is handled
		   AfterHandleRequestResponseAsync = (message, method, id) =>
		   {
			   ActorEventSource.Current.Message($"AfterHandleRequestResponseAsync {method} for actor {id.ToString()}");
			   return Task.CompletedTask;
		   }
	   };
	   return service;
   }).GetAwaiter().GetResult();
```
### Sender

On the sender side, use the `ExtendedServiceProxy` or `ExtendedActorProxy` class to create a proxy to the actor or service. The `Create` method accepts an instance of the `CustomHeaders` class:

```csharp
var customHeaders = new CustomHeaders
{
	{"Header1", DateTime.Now.ToString(CultureInfo.InvariantCulture)},
	{"Header2", Guid.NewGuid().ToString()}
};

var serviceUri = new Uri("fabric:/ServiceFabric.Remoting.CustomHeaders.DemoApplication/DemoService");
var proxy = ExtendedServiceProxy.Create<IDemoService>(serviceUri, customHeaders);
var actorMessage = proxy.SayHello().GetAwaiter().GetResult();
```       

There is an overload of the `Create` method that accepts a `Func<CustomHeaders>`. This is useful in scenarios where the created proxy is reused. The func is invoked on every request made using the proxy:

```csharp
var customHeaderProvider = new Func<CustomHeaders>(() => new CustomHeaders
{
	{"Header1", DateTime.Now.ToString(CultureInfo.InvariantCulture)},
	{"Header2", Guid.NewGuid().ToString()}
});
var serviceUri = new Uri("fabric:/ServiceFabric.Remoting.CustomHeaders.DemoApplication/DemoService");
var proxy = ExtendedServiceProxy.Create<IDemoService>(serviceUri, customHeaderProvider);
```
### Receiver

The receiving service or actor can extract the values in the custom headers using the `RemotingContext` class:

```csharp
public async Task<string> SayHello()
{
	var remotingContext =
		string.Join(", ", RemotingContext.Keys.Select(k => $"{k}: {RemotingContext.GetData(k)}"));

	ServiceEventSource.Current.ServiceMessage(Context, $"SayHelloToActor got context: {remotingContext}");
	return Task.FromResult($"Got the following message headers: {remotingContext}")
}
```

Sample content of remotingContext: 

> Header1: 06/24/2018 08:30:18, Header2: 2c95548a-6efd-4855-82eb-29ea827be87b
