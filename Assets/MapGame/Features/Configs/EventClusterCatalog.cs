using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Raccolta di EventClusterShape disponibili per il piazzamento. Popolabile a mano
    /// in Inspector trascinando le forme, oppure con il bottone "Carica tutte le forme
    /// dalla cartella" nel suo Editor custom (scansiona Assets/MapGame/Content/EventClusters/).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Event Cluster Catalog", fileName = "EventClusterCatalog")]
    public class EventClusterCatalog : ScriptableObject
    {
        public EventClusterShape[] Shapes;
    }
}
