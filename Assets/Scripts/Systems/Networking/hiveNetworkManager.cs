using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Reflection;
using UnityEngine.Events;

public class HiveNetworkManager : NetworkManager
{
    [SerializeField] private HivePlayer GamePlayerPrefab;

    [Tooltip("Alive players by their network connection")]
    [SerializeField] HivePlayerDictionary alivePlayersByConnection;

    [Tooltip("All players by their network connection")]
    [SerializeField] HivePlayerDictionary playersByConnection;

    [Tooltip("All players in the game")]
    [SerializeField] HivePlayerSet allPlayers;

    int playersLoaded = 0;

    [Tooltip("The number of players in the game")]
    [SerializeField] IntVariable playerCount;

    [Tooltip("The event to invoke when a player loads into the game scene")]
    [SerializeField] NetworkingEvent playerLoaded;

    [Tooltip("The event to invoke when all players have loaded into the game")]
    [SerializeField] GameEvent allPlayersLoaded;

    [Tooltip("The scene we consider to be the game scene")]
    [field:SerializeField]
    public GameModeVariable GameMode;

    [Tooltip("The scene we consider to be the lobby scene")]
    [Scene]
    public string LobbyScene;

    #region Connection Events
    [Tooltip("Invoked on the client when they connect to the server")]
    [SerializeField] GameEvent onClientConnect;

    [Tooltip("Invoked on the client when they disconnect from the server")]
    [SerializeField] GameEvent onClientDisconnect;

    [Tooltip("Invoked on the server when a player connects")]
    [SerializeField] NetworkingEvent onServerConnect;

    [Tooltip("Invoked on the server when a player disconnects")]
    [SerializeField] NetworkingEvent onServerDisconnect;

    /// <summary>
    /// The ID of the most recent player to join the lobby. I am aware that this is a bodged fix for now. I just want a stable build xd
    /// </summary>
    ulong lastId;
    #endregion

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<RequestIDMsg>(ReceiveSteamID);
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<RequestIDMsg>(OnIDRequested);
    }

    public override void OnStopClient()
    {
        SteamMatchmaking.LeaveLobby(SteamLobby.LobbyID);
    }

    /// <summary>
    /// Called when a player joins the server for the first time
    /// </summary>
    /// <param name="conn"></param>
    [Server]
    public void OnPlayerJoin(NetworkConnection conn)
    {
        Debug.Log($"Player {SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, SteamLobby.LobbySize - 1))} connected");

        if (SceneManager.GetActiveScene().path == GameMode.Value.GameScene)
        {
            HivePlayer ply = null;
            ulong id = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, SteamLobby.LobbySize - 1);

            foreach (HivePlayer i in allPlayers.Value)
            {
                ply = i;
                if (ply.PlayerID == id) break;
                ply = null;
            }

            if (ply != null)
            {
                //Assign the player back their HoLPlayer object if they reconnect to the game
                ply.netIdentity.AssignClientAuthority(conn);
                return;
            }

            CreateSpectator(conn, id);

            onServerConnect?.Invoke(conn);
        }

        if (SceneManager.GetActiveScene().path == LobbyScene)
        {
            conn.Send(new RequestIDMsg());
        }
    }

    [Server]
    void ReceiveSteamID(NetworkConnection conn, RequestIDMsg msg)
    {
        CreatePlayer(conn, msg.ID, msg.username);
        onServerConnect?.Invoke(conn);
    }

    [Client]
    void OnIDRequested(RequestIDMsg msg)
    {
        msg.ID = (ulong) SteamUser.GetSteamID();
        msg.username = SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID());
        NetworkClient.Send(msg);
    }

    void CreateSpectator(NetworkConnection conn, ulong id)
    {
        HivePlayer ply = Instantiate(GamePlayerPrefab);
        ply.PlayerID = id;
        ply.DisplayName = SteamFriends.GetFriendPersonaName(new CSteamID(id));
        ply.gameObject.name = "Spectator: " + ply.DisplayName;
        playersByConnection.Value[conn] = ply;
        allPlayers.Add(ply);
        DontDestroyOnLoad(ply.gameObject);
        NetworkServer.AddPlayerForConnection(conn, ply.gameObject);
    }

    public void CreatePlayer(NetworkConnection conn, ulong id, string userName)
    {
        HivePlayer ply = Instantiate(GamePlayerPrefab);

        ply.PlayerID = id;
        ply.DisplayName = userName;
        ply.gameObject.name = "Player: " + ply.DisplayName;
        playersByConnection.Value[conn] = ply;
        alivePlayersByConnection.Value[conn] = ply;
        allPlayers.Add(ply);
        DontDestroyOnLoad(ply.gameObject);

        NetworkServer.AddPlayerForConnection(conn, ply.gameObject);
    }

    [Server]
    public override void OnStopServer()
    {
        allPlayers.Value = new();
        alivePlayersByConnection.Value = new();
        playersByConnection.Value = new();
    }

    [Server]
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        onServerDisconnect?.Invoke(conn);

        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        playersByConnection.Value.Remove(conn);
        if (alivePlayersByConnection.Value.TryGetValue(conn, out HivePlayer pl)) alivePlayersByConnection.Value.Remove(conn);
        allPlayers.Remove(ply);

        //Can't prevent the player object being destroyed when a player leaves the game, so we need to make a new object to be destroyed instead
        HivePlayer newObj = Instantiate(GamePlayerPrefab);
        NetworkServer.ReplacePlayerForConnection(conn, newObj.gameObject);

        NetworkServer.DestroyPlayerForConnection(conn);
    }

    [Server]
    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        onServerConnect.Invoke(conn);
        OnPlayerJoin(conn);
    }

    [Client]
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        onClientDisconnect.Invoke();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        onClientConnect.Invoke();
    }

    [Server]
    public void StartGame()
    {
        ServerChangeScene(GameMode.Value.GameScene);
        playerCount.Value = allPlayers.Value.Count;
        Debug.Log($"Started game with {playerCount.Value} players");
    }

    [Server]
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        if (SceneManager.GetActiveScene().path != GameMode.Value.GameScene) return;

        playerLoaded?.Invoke(conn);

        if (++playersLoaded < playerCount) return;

        StartCoroutine(Coroutines.Delay(0.5f, () =>
        {
            alivePlayersByConnection.Value = new();
            foreach(HivePlayer ply in allPlayers.Value)
            {
                alivePlayersByConnection.Value[ply.connectionToClient] = ply;
            }
            allPlayersLoaded.Invoke();
            //For if we want to reset the game
            playersLoaded = 0;
        }));
    }

    [Server]
    public override void OnServerChangeScene(string scene)
    {
        playerCount.Value = allPlayers.Value.Count;
        if (scene != GameMode.Value.GameScene) return;

        allPlayers.Value.ForEach(ply => ply.ResetValues());

        Object[] variables = Resources.LoadAll("Variables");

        for (int i = 0; i < variables.Length; i++)
        {
            System.Type variableType = variables[i].GetType();

            bool isVariable = IsAssignableToGenericType(variableType, typeof(Variable<>));
            bool isSet = IsAssignableToGenericType(variableType, typeof(RuntimeSet<>));

            if (isVariable) variableType.GetMethod(nameof(Variable<int>.OnEnable))?.Invoke(variables[i], new object[] { });
            if (isSet) variableType.GetMethod(nameof(RuntimeSet<int>.ClearSet))?.Invoke(variables[i], new object[] { });
        }
    }

    [Server]
    public static bool IsAssignableToGenericType(System.Type givenType, System.Type genericType)
    {
        var interfaceTypes = givenType.GetInterfaces();

        foreach (var it in interfaceTypes)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                return true;
        }

        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            return true;

        System.Type baseType = givenType.BaseType;
        if (baseType == null) return false;

        return IsAssignableToGenericType(baseType, genericType);
    }

    private struct RequestIDMsg : NetworkMessage
    {
        public ulong ID;
        public string username;
    }
}