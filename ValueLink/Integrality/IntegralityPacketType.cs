// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ValueLink.Integrality;

/// <summary>
/// Identifies the kind of an integrality protocol packet (the first byte of the packet).
/// </summary>
public enum IntegralityPacketType : byte
{
    /// <summary>
    /// A request carrying the source owner's hash.
    /// </summary>
    Probe,

    /// <summary>
    /// A response carrying the target owner's hash and its key/hash list.
    /// </summary>
    ProbeResponse,

    /// <summary>
    /// A request for the objects with the listed keys.
    /// </summary>
    Get,

    /// <summary>
    /// A response carrying the requested objects.
    /// </summary>
    GetResponse,
}
