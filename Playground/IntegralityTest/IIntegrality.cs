namespace Tinyhand;

public interface IIntegrality
{// Exaltation of the Integrality by Baxter.
    ulong RootHash { get; set; }

    ulong GetRootHash();

    void ClearRootHash();
}
