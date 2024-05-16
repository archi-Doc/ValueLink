namespace Tinyhand.Integrality;

public class IntegralityEngine
{
    public IntegralityEngine()
    {
    }

    public int Level { get; private set; }

    public void GetIdentity(IIntegrality integrality)
    {
        if (this.Level == 0)
        {// Get root hash
            var rootHash = integrality.GetRootHash();
        }
    }
}
