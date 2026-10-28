// Scripts/Anchors/TrackingSpaceAligner.cs
// Role: Monitors player properties on Photon and pipes the remote
//       Meta User ID into SharedAnchorManager when it arrives.
//       Lives on the same GameObject as SharedARNetworkManager.

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class TrackingSpaceAligner : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        // Check if we already have everyone's properties (late join scenario)
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.IsLocal)
                TryExtractRemoteUserId(player);
        }
    }

    // Fires whenever ANY player updates their custom properties
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer.IsLocal) return; // ignore our own updates
        TryExtractRemoteUserId(targetPlayer, changedProps);
    }

    private void TryExtractRemoteUserId(Player player, Hashtable props = null)
    {
        var source = props ?? player.CustomProperties;
        if (source.TryGetValue("MetaUID", out object val) && val != null)
        {
            if (ulong.TryParse(val.ToString(), out ulong remoteId))
            {
                Debug.Log($"[Aligner] Remote Meta UID captured: {remoteId}");
                SharedAnchorManager.Instance.OnRemotePlayerMetaId(remoteId);
            }
        }
    }
}
