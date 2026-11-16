namespace Volumetric.Core.Interfaces;

public interface IBackgroundScanTaskRegistrar
{
    void EnsureRegistered();
    void ReRegister();
}
