namespace DataLinkApplication
{
  public class GoBacknProtocol : ProtocolBase, IProtocol
  {
    #region Implementation of IProtocol

    public void Protocol()
    {
      // Traduire le protocol 5 dans le livre de Tanenbaum en C#
    }

    public void FromNetworkLayer(Packet packet)
    {
      // Un packet pour l'émetteur (lu du fichier d'entrée)
    }

    public void FromPhysicalLayer(Frame frame)
    {
      // Une trame pour le récepteur (à écrire dans le fichier de sortie)
    }

    #endregion
  }
}
