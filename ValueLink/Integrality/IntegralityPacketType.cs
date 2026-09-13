// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1602 // Enumeration items should be documented

namespace ValueLink.Integrality;

public enum IntegralityState : byte
{
    Probe,
    ProbeResponse,
    Get,
    GetResponse,
}
