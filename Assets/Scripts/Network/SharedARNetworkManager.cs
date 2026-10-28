// Scripts/Network/SharedARNetworkManager.cs
// Role: Connects both headsets to Photon, manages room state,
//       and relays the shared anchor UUID between players.

using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SharedARNetworkManager : MonoBehaviourPunCallbacks
{
    public static SharedARNetworkManager Instance { get; private set; }

    [Header("Room Config")]
    [SerializeField] private string roomName = "SharedARRoom";
    [SerializeField] private byte maxPlayers = 2;

    // Key used to broadcast the host's shared anchor UUID through room properties
    private const string ANCHOR_UUID_PROP = "AnchorUUID";

    // Events other systems subscribe to
    public Action OnBothPlayersReady;
    public Action<string> OnAnchorUUIDBroadcast; // fires on non-host when UUID arrives

    public bool IsHost => PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        Debug.Log("[Network] Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // ── Photon Callbacks ─────────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Network] Connected. Joining or creating room...");
        var options = new RoomOptions
        {
            MaxPlayers    = maxPlayers,
            IsVisible     = true,
            IsOpen        = true,
            CustomRoomProperties           = new Hashtable { { ANCHOR_UUID_PROP, "" } },
            CustomRoomPropertiesForLobby   = new[] { ANCHOR_UUID_PROP }
        };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Network] In room. IsMaster={IsHost} | Players={PhotonNetwork.CurrentRoom.PlayerCount}");

        // Spawn our networked avatar immediately
        PhotonNetwork.Instantiate("PlayerAvatar", Vector3.zero, Quaternion.identity);

        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
            OnBothPlayersReady?.Invoke();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Network] Player joined → total: {PhotonNetwork.CurrentRoom.PlayerCount}");
        if (PhotonNetwork.CurrentRoom.PlayerCount == maxPlayers)
            OnBothPlayersReady?.Invoke();
    }

    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        // Non-host receives the anchor UUID here and triggers the load
        if (!IsHost && changed.ContainsKey(ANCHOR_UUID_PROP))
        {
            string uuid = changed[ANCHOR_UUID_PROP].ToString();
            if (!string.IsNullOrEmpty(uuid))
            {
                Debug.Log($"[Network] Received anchor UUID from host: {uuid}");
                OnAnchorUUIDBroadcast?.Invoke(uuid);
            }
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Host calls this after saving anchor to cloud.</summary>
    public void BroadcastAnchorUUID(string uuid)
    {
        var props = new Hashtable { { ANCHOR_UUID_PROP, uuid } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log($"[Network] Anchor UUID broadcasted: {uuid}");
    }

    public override void OnDisconnected(DisconnectCause cause)
        => Debug.LogWarning($"[Network] Disconnected: {cause}");
}
