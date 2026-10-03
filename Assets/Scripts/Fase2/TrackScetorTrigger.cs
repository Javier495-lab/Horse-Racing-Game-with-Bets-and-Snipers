using UnityEngine;

public class TrackScetorTrigger : MonoBehaviour
{
    public int sectorIndex = 0;      // 0 para el primer tramo, 1 para el segundo, etc.
    public int totalSectorsInTrack = 4; // Número total de checkpoints en el circuito

    private void OnTriggerEnter(Collider other)
    {
        HorseRunner horse = other.GetComponent<HorseRunner>();
        if (horse != null)
        {
            horse.OnEnterTrackSector(sectorIndex, totalSectorsInTrack);
        }
    }
}
