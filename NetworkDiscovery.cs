using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Transporting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// microsoft java
using System.Runtime.InteropServices;

namespace FishNet.Discovery
{
	// bu struct ile sunucuyu tanıtıyoruz, 
	// istediğinizi ekleyin (basit şeyler int, float, string falan)
	// oyuncu adı, odada kaç kişi olduğu, kaç dk sürenin kaldığı vs gönderilebilir
	
    [System.Serializable]
    public struct ServerInfoPacktet
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string PlayerName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string ipAdress;
    }

    public sealed class NetworkDiscovery : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("A string that differentiates your application/game from others. Must not be null, empty, or blank.")]
        string secret;

        [SerializeField]
        [Tooltip("The port number used by this NetworkDiscovery component. Must be different from the one used by the Transport.")]
        ushort port;

        [SerializeField]
        [Tooltip("How often does this NetworkDiscovery component advertises a server or searches for servers.")]
        float discoveryInterval;

        [SerializeField]
        [Tooltip("Whether this NetworkDiscovery component will automatically start/stop? Setting this to true is recommended.")]
        bool automatic;

        UdpClient _serverUdpClient;
        UdpClient _clientUdpClient;
        public bool IsAdvertising => _serverUdpClient != null;
        public bool IsSearching => _clientUdpClient != null;
        public event Action<ServerInfoPacktet> ServerFoundCallback;

        #region "Event setup"

        private void Start()
        {
            if (automatic)
            {
                InstanceFinder.ServerManager.OnServerConnectionState += ServerConnectionStateChangedHandler;
                InstanceFinder.ClientManager.OnClientConnectionState += ClientConnectionStateChangedHandler;
                StartSearchingForServers();
            }
        }

        private void OnDisable()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;
            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;
            StopAdvertisingServer();
            StopSearchingForServers();
        }

        private void OnDestroy()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;
            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;
            StopAdvertisingServer();
            StopSearchingForServers();
        }

        private void OnApplicationQuit()
        {
            InstanceFinder.ServerManager.OnServerConnectionState -= ServerConnectionStateChangedHandler;
            InstanceFinder.ClientManager.OnClientConnectionState -= ClientConnectionStateChangedHandler;
            StopAdvertisingServer();
            StopSearchingForServers();
        }

        #endregion

        #region Connection State Handlers

        private void ServerConnectionStateChangedHandler(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Starting) { StopSearchingForServers(); }
            else if (args.ConnectionState == LocalConnectionState.Started) { StartAdvertisingServer(); }
            else if (args.ConnectionState == LocalConnectionState.Stopping) { StopAdvertisingServer(); }
            else if (args.ConnectionState == LocalConnectionState.Stopped) { StartSearchingForServers(); }
        }

        private void ClientConnectionStateChangedHandler(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Starting) { StopSearchingForServers(); }
            else if (args.ConnectionState == LocalConnectionState.Stopped) { StartSearchingForServers(); }
        }

        #endregion

        #region Server

        public void StartAdvertisingServer()
        {
            if (!InstanceFinder.IsServer || _serverUdpClient != null || port == InstanceFinder.TransportManager.Transport.GetPort())
            {
                if (NetworkManager.StaticCanLog(LoggingType.Warning)) Debug.LogWarning("Sunucu reklam yapamıyo", this);
                return;
            }

            _serverUdpClient = new UdpClient(port)
            {
                EnableBroadcast = true,
                MulticastLoopback = false,
            };

            Task.Run(AdvertiseServerAsync);
            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Sunucu reklama başladı.", this);
        }

        public void StopAdvertisingServer()
        {
            if (_serverUdpClient == null) return;
            _serverUdpClient.Close();
            _serverUdpClient = null;
            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Sunucu reklamı kapattı.", this);
        }


        // !! sunucu, reklam yapıyor. (bişey yollamıyor client sorarsa cevap veriyo sadece)
        private async void AdvertiseServerAsync()
        {
            while (_serverUdpClient != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(discoveryInterval));

                UdpReceiveResult result = await _serverUdpClient.ReceiveAsync();

                string receivedSecret = Encoding.UTF8.GetString(result.Buffer);

                // ! gelen cevap, secret ile aynı ise
				// burda mesela oda şifresi de olabilir
                if (receivedSecret == secret)
                {

                    ServerInfoPacktet info = new ServerInfoPacktet();
                    info.PlayerName = "TEST SERVER"; // buraya kuran kişinin ismi gelebilir
                    byte[] okBytes = getBytes(info);

                    // ! sunucuyu tanıt
                    await _serverUdpClient.SendAsync(okBytes, okBytes.Length, result.RemoteEndPoint);
                }
            }
        }

        #endregion

        #region Client

        public void StartSearchingForServers()
        {
            if (InstanceFinder.IsServer || InstanceFinder.IsClient || _clientUdpClient != null)
            {
                if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Sunucu arayamıyoz.", this);
                return;
            }

            _clientUdpClient = new UdpClient()
            {
                EnableBroadcast = true,
                MulticastLoopback = false,
            };

            Task.Run(SearchForServersAsync).ConfigureAwait(true);
            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Sunucular aranıyor.", this);
        }

        public void StopSearchingForServers()
        {
            if (_clientUdpClient == null) return;
            _clientUdpClient.Close();
            _clientUdpClient = null;
            if (NetworkManager.StaticCanLog(LoggingType.Common)) Debug.Log("Sunucu aramayı bıraktık", this);
        }

        // ! client, sunucu arıyor
        private async void SearchForServersAsync()
        {
            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, port);

            while (_clientUdpClient != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(discoveryInterval));

                // ! secreti yolla 
                await _clientUdpClient.SendAsync(secretBytes, secretBytes.Length, endPoint);

                UdpReceiveResult result = await _clientUdpClient.ReceiveAsync();

                // ! gelen cevap 1 ise Eventi uyandırıyordu normalde
				// ama bu mantık yine kullanılabilir, mesela oyunun dolu olması veya odaya şifre ayarlamak
                /*if (BitConverter.ToBoolean(result.Buffer, 0))
                {
                    ServerFoundCallback?.Invoke(result.RemoteEndPoint);

                    StopSearchingForServers();
                }*/
                ServerInfoPacktet info = fromBytes(result.Buffer);
                info.ipAdress = result.RemoteEndPoint.Address.ToString();
				
				// Dispacther.cs githubda var
                Dispatcher.RunOnMainThread(() => ServerFoundCallback?.Invoke(info));
            }
        }

        #endregion
    #region  "Marshall"

    byte[] getBytes(ServerInfoPacktet str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;
    }

    ServerInfoPacktet fromBytes(byte[] arr)
    {
        ServerInfoPacktet str = new ServerInfoPacktet();
        int size = Marshal.SizeOf(str);
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(arr, 0, ptr, size);
            str = (ServerInfoPacktet)Marshal.PtrToStructure(ptr, str.GetType());
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return str;
    }

    #endregion
    }

}