namespace WinDirStat.Core.Interfaces;

public interface IBackgroundScanTaskRegistrar
{
    void EnsureRegistered();
    void ReRegister();
}
