namespace Volumetric.Core.Entities;

public sealed class ScanResultKeyUnavailableException()
    : Exception("The scan result is encrypted and the decryption key is not available on this device.");
