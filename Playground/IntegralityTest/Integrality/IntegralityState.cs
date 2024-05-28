namespace ValueLink.Integrality;

internal enum IntegralityState : byte
{
    Probe,
    ProbeResponse,
    Get,
    GetResponse,
}
