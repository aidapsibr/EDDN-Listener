# EDDN Listener

Connects to [EDDN (Elite:Dangerous Data Network)](https://github.com/jamesremuscat/EDDN), a ZeroMQ Relay with ZLib compression, and executes a callback when messages are received and provides the decompressed JSON to the callback.

This is an extremely easy way to get started with EDDN in C#. 

The operations are fully TPL (Task-Parallel Library) threaded and async, as such make sure the main thread lives on after calling the BeginListener (Thread.Sleep(5000000) for instance).


```csharp
EddnListener
  .Create()
  .BeginListener((message) => Console.WriteLine(message));
  
Thread.Sleep(500000)  //This is only a holder for the main thread, only necessary in console apps that do nothing else.
```
