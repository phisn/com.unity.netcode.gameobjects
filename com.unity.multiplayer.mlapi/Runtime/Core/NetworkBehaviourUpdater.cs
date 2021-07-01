using System.Collections.Generic;
using Unity.Profiling;

namespace MLAPI
{
    public class NetworkBehaviourUpdater
    {
        private HashSet<NetworkObject> m_Touched = new HashSet<NetworkObject>();

        /// <summary>
        /// Stores the network tick at the NetworkBehaviourUpdate time
        /// This allows sending NetworkVariables not more often than once per network tick, regardless of the update rate
        /// </summary>
        public ushort CurrentTick { get; set; }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private ProfilerMarker m_NetworkBehaviourUpdate = new ProfilerMarker($"{nameof(NetworkBehaviour)}.{nameof(NetworkBehaviourUpdate)}");
#endif

        internal void NetworkBehaviourUpdate(NetworkManager networkManager)
        {
            // Do not execute NetworkBehaviourUpdate more than once per network tick
            ushort tick = networkManager.NetworkTickSystem.GetTick();
            if (tick == CurrentTick)
            {
                return;
            }

            CurrentTick = tick;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_NetworkBehaviourUpdate.Begin();
#endif
            try
            {
                if (networkManager.IsServer)
                {
                    m_Touched.Clear();
                    for (int i = 0; i < networkManager.ConnectedClientsList.Count; i++)
                    {
                        var client = networkManager.ConnectedClientsList[i];
                        var spawnedObjs = networkManager.SpawnManager.SpawnedObjectsList;
                        m_Touched.UnionWith(spawnedObjs);
                        foreach (var sobj in spawnedObjs)
                        {
                            // Sync just the variables for just the objects this client sees
                            for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                            {
                                sobj.ChildNetworkBehaviours[k].VariableUpdate(client.ClientId);
                            }
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in m_Touched)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
                else
                {
                    // when client updates the server, it tells it about all its objects
                    foreach (var sobj in networkManager.SpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].VariableUpdate(networkManager.ServerClientId);
                        }
                    }

                    // Now, reset all the no-longer-dirty variables
                    foreach (var sobj in networkManager.SpawnManager.SpawnedObjectsList)
                    {
                        for (int k = 0; k < sobj.ChildNetworkBehaviours.Count; k++)
                        {
                            sobj.ChildNetworkBehaviours[k].PostNetworkVariableWrite();
                        }
                    }
                }
            }
            finally
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                m_NetworkBehaviourUpdate.End();
#endif
            }
        }

    }
}