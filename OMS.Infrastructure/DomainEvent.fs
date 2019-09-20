namespace OMS.Infrastructure

open System

/// Specifies the versioning information for a domain event, using 
type Versioning =
    | SequenceNumber of int64
    | ETag of string

/// Describes a discrete event in the system, where the data and metadata are serialized to bytes
type DomainEvent = {
    Id : Guid
    Name : string
    Data : byte []
    Metadata : byte []
    Version : Versioning option
    Timestamp : DateTimeOffset option
} with
    static member New(id, name, data, metadata) = {
        Id = id
        Name = name
        Data = data
        Metadata = metadata
        Version = None
        Timestamp = None
    }
