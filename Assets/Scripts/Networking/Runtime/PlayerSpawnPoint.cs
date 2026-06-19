using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Networking
{
    /// <summary>
    /// Optional marker placed in the gameplay scene to control where networked players appear.
    /// Add this to one or more empty GameObjects positioned on the floor; <see cref="NetworkPlayerSpawner"/>
    /// will cycle through them when spawning players. If no markers exist, the spawner falls back to a
    /// configured default position.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        private static readonly List<PlayerSpawnPoint> Points = new List<PlayerSpawnPoint>();

        /// <summary>Spawn points currently enabled in any loaded scene.</summary>
        public static IReadOnlyList<PlayerSpawnPoint> ActivePoints => Points;

        private void OnEnable()
        {
            if (!Points.Contains(this))
                Points.Add(this);
        }

        private void OnDisable()
        {
            Points.Remove(this);
        }
    }
}
