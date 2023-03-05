using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;

public class NameManager : NetworkBehaviour
{
    // ! her client'de biri adını değiştirince çağrılır, 
    // * hem isimleri değiştirmek, hemde diğerlerini bildirmek için kullanılabilir
    public static event Action<NetworkConnection, string> OnNameChange;

    // * oyundaki enayiler (oyuncular işte)
    [SyncObject]
    private readonly SyncDictionary<NetworkConnection, string> _playerNames = new SyncDictionary<NetworkConnection, string>();

    private static NameManager _instance;

    private void Awake()
    {
        _instance = this;
        _playerNames.OnChange += _playerNames_OnChange;
    }

    #region "sunucudan biri çıkınca oyuncuları çıkarmak için callback"
    public override void OnStartServer()
    {
        base.OnStartServer();
        base.NetworkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        base.NetworkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
    }
    #endregion

    // ! biri oyundan çıkınca çağrılır, sadece sunucuda çağrılır
    private void ServerManager_OnRemoteConnectionState(NetworkConnection arg1, FishNet.Transporting.RemoteConnectionStateArgs arg2)
    {
        if (arg2.ConnectionState != RemoteConnectionState.Started)
            _playerNames.Remove(arg1);

        
    }

    // ! sunucu listesi değişince her client'de çağrılır
    private void _playerNames_OnChange(SyncDictionaryOperation op, NetworkConnection key, string value, bool asServer)
    {
        // sunucu listesi değişince kendiside otomatik ayarlıyo listeyi 
        if (op == SyncDictionaryOperation.Add || op == SyncDictionaryOperation.Set)
            // ! eventi uyandır
            OnNameChange?.Invoke(key, value);
    }

    // ! NetworkConnection ile oyuncu adını getir
    // * hem client, hem serverde kullanılabilir
    public static string GetPlayerName(NetworkConnection conn)
    {
        if (_instance._playerNames.TryGetValue(conn, out string result))
            return result;
        else
            return string.Empty;
    }

    // * clientlerin kendi adını ayarlaması için
    [Client]
    public static void SetName(string name)
    {
        _instance.ServerSetName(name);
    }

    // ! ad ayarlamak için server rpc
    [ServerRpc(RequireOwnership = false)]
    private void ServerSetName(string name, NetworkConnection sender = null)
    {
        // dict'e ekliyo o kadar
        // syncdict olduğu için _playerNames_OnChange otomatik olarak çağrılıyor
        _playerNames[sender] = name;
    }
}
