NOTICE: SUPPORT FOR THIS PROJECT ENDED ON 18 November 2020

This projected was owned and maintained by Jet.com (Walmart). This project has reached its end of life and Walmart no longer supports this project.

We will no longer be monitoring the issues for this project or reviewing pull requests. You are free to continue using this project under the license terms or forks of this project at your own risk. This project is no longer subject to Jet.com/Walmart's bug bounty program or other security monitoring.


## Actions you can take

We recommend you take the following action:

  * Review any configuration files used for build automation and make appropriate updates to remove or replace this project
  * Notify other members of your team and/or organization of this change
  * Notify your security team to help you evaluate alternative options

## Forking and transition of ownership

For [security reasons](https://www.theregister.co.uk/2018/11/26/npm_repo_bitcoin_stealer/), Walmart does not transfer the ownership of our primary repos on Github or other platforms to other individuals/organizations. Further, we do not transfer ownership of packages for public package management systems.

If you would like to fork this package and continue development, you should choose a new name for the project and create your own packages, build automation, etc.

Please review the licensing terms of this project, which continue to be in effect even after decommission.

ORIGINAL README BELOW

----------------------

# OMS Infrastructure example repository

This repository demonstrates some of the concept's used by Jet's Order Management System
to abstract the concerns of connecting to and interacting with infrastructure components
using F#. It should not be treated as production-ready code. Instead, use it as a guide
for how to build a similar abstraction layer.

To learn more about the concepts in this example repo, please read the following articles:

* [Abstracting IO using F#](https://medium.com/jettech/abstracting-io-using-f-dc841519610e)
* [F# Microservice Patterns](https://medium.com/jettech/f-microservice-patterns-jet-com-98b5f7025423)
* [Scaling Microservices](https://medium.com/jettech/scaling-microservices-jet-com-4a5bf0eaad92)
* [Observability Using Abstracted IO](https://medium.com/jettech/observability-using-abstracted-io-8689bcf31fea)

## Security
This code should not be used in production and is for demo/sample purposes only. Please see [SECURITY.md](SECURITY.md) for information on reporting security concerns.

## Concepts

The OMS.Infrastructure project is a demonstration of how to abstract away
common I/O concerns using concepts from Domain Driven Design, and 
the Repository Pattern.

Quite simply, target infrastructure is distilled down to a string URI.
```
let uri = "kafka://kafa-brokers/some-topic?someOption=true
```

The URI describes the target infrastructure, and the library uses this
string to generate an object known as a Stream Definition. The Stream
Definition can then be used to access the infrastructure for reads and
writes.

Ex:
```
let sd = uri |> Service.parse
let result = Inputs.read sd
metch result with
| Choice1Of2 ReadResult.Found events -> () // content is available
| Choiec2Of2 ReadResult.NotFound -> () // no content was found
```

The Inputs and Outputs modules use a common data structure called the
Domain Event, which wraps the data read, or data written as a byte array.

ex:
```
let data = events |> List.map (fun e -> e |> deserializeFromBytes<MyData>)
```

All read and write operations handle logging and metric writing in a
consistent manner. There is no need for any infrastructure adapter to
implement this logic. It only needs to return the data that was reqeusted
of it.

ex:
```
module OMS.Infrastructure.Inputs

let read (sd:StreamDefinition) =
    // start by return a read result for the appropriate Stream Definition
    readForSd sd
    |> Async.retry
    |> Async.Catch
    |> Logging.logException
    |> Metrics.recordLatency
```

Lastly, the generalized details for the microservice pattern used with
this repository are also described in the [Service.fs](OMS.Infrastructure/Service.fs) file.

Microservices were treated as stream processors following the Single 
Responsibility Principle. Messages arriving at the microservice are processed
and a side effect is generated as an output. There are three main components of the 
pipeline:

`decode` (deserialize message) -> `handle` (apply business logic) -> `interpret` (execute side-effect)

Example setup of a microservice
```
let handler = Service.processInput log "MicroServiceName" handle interpret

incomingStreamDefinitionUri
|> Service.parse lookup
|> Service.consume 
|> Service.decode microserviceDecoder
|> Service.handle log handler
|> Service.start
```

These interfaces and patterns were used to reduce the complexity of the OMS code base
and made the parlance of interacting with infrastructure simple, concise, and easy to learn.

## Files

**OMS.Infrastructure**

File | Description
--- | ---
StreamDefinitions.fs | Describes the details needed to create connections to relevant infrastructure
DomainEvent.fs | Defines a discrete event in the system as a byte array
Definitions.fs | General type definitions used within the module
Logging.fs | Implements the details for logging exceptions and events
Async.fs | Stubbed definitions for retry logic
Metrics.fs | Defines how metrics are captured and emitted
Inputs.fs | Standard interfaces for reading from infrastructure
Outputs.fs | Standard interfaces for writing to infrastructure
Service.fs | Describes a set of interfaces for the OMS microservice patterns

**Microservice**

File | Description
--- | ---
Microservice.fs | An example microservice that consumes and processes messages from a stream

