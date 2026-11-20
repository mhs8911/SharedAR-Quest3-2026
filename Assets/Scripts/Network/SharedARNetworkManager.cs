// ============================================================================
// Mariam code addition
// File: Scripts/Network/SharedARNetworkManager.cs
// Changes from your version:
//  [CHANGED] No longer a NetworkBehaviour. It had NetworkVariables but was not on a
//            spawned NetworkObject, so they could not sync. It is now a plain
//            MonoBehaviour that starts and stops the connection.
//  [REMOVED] anchorPosition / anchorRotation NetworkVariables, SetAnchorReady,
//            RPC_BroadcastAnchor (it did not end in "Rpc", so it will not compile in
//            NGO 2.x), IsAnchorReady. Each headset detects the QR code itself, and
//            sharing a world-space anchor between devices would be wrong.
//  [REMOVED] DontDestroyOnLoad: it would break the AlignmentManager reference
//            when the scene reloads. NetworkManager itself persists (put it in Bootstrap).
//  [KEPT]    StartAsHost(), StartAsClient(string), Instance, address/port fields.
//  [ADDED]   alignment gate, host listen on 0.0.0.0, local IP display, controller
//            shortcuts (X = host, Y = join), Shutdown(), StatusMessage.
// ============================================================================
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SharedARNetworkManager : MonoBehaviour
{
    public static SharedARNetworkManager Instance { get; private set; }

    //change to host's IP when client is on different device.
    [SerializeField] private string connectionAddress = "127.0.0.1";
    [SerializeField] private ushort connectionPort = 7777;

    // [ADDED]
    [SerializeField] private AlignmentManager alignmentManager;
    [SerializeField] private bool requireAlignmentBeforeConnect = true;
    [SerializeField] private bool enableControllerShortcuts = true;   // X = host, Y = join

    public string StatusMessage { get; private set; } = "Idle";
    public string LocalIp { get; private set; } = "unknown";

    public bool CanConnect =>
        !requireAlignmentBeforeConnect
        || (alignmentManager != null && alignmentManager.IsAligned);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // [REMOVED] DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LocalIp = GetLocalIPv4();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;   // [ADDED]
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
        else
        {
            StatusMessage = "No NetworkManager in scene (put it in Bootstrap)";
        }
    }

    private void Update()
    {
        if (!enableControllerShortcuts) return;
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening) return;

        if (OVRInput.GetDown(OVRInput.Button.Three)) StartAsHost();
        if (OVRInput.GetDown(OVRInput.Button.Four)) StartAsClient(connectionAddress);
    }

    public void StartAsHost()
    {
        if (NetworkManager.Singleton == null) return;
        if (!CanConnect)
        {
            StatusMessage = "Scan the QR marker first";
            Debug.LogWarning("[Net] Not aligned yet.");
            return;
        }

        // [ADDED] listen on all interfaces so the other Quest can reach this one
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetConnectionData("127.0.0.1", connectionPort, "0.0.0.0");

        NetworkManager.Singleton.StartHost();
        StatusMessage = $"Hosting on {LocalIp}:{connectionPort}";
        Debug.Log("Started as Host");
    }

    public void StartAsClient(string serverAddress)
    {
        if (NetworkManager.Singleton == null) return;
        if (!CanConnect)
        {
            StatusMessage = "Scan the QR marker first";
            Debug.LogWarning("[Net] Not aligned yet.");
            return;
        }

        connectionAddress = serverAddress;
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetConnectionData(serverAddress, connectionPort);
        NetworkManager.Singleton.StartClient();
        StatusMessage = $"Connecting to {serverAddress}...";
        Debug.Log($"Connecting to {serverAddress}");
    }

    // [ADDED]
    public void Shutdown()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        StatusMessage = "Idle";
    }

    private void OnServerStarted()
    {
        Debug.Log("Server started");   // [CHANGED] original: "waiting for anchor data"
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            StatusMessage = "Connected to host";
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
    }

    private static string GetLocalIPv4()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);   // UDP connect: no packet is actually sent
                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
        }
        catch { return "unknown"; }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}
