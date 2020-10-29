using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public enum ClientStatus {
    disconnected,
    connecting,
    login,
    sendLogin,
    connected
}

public class Client : MonoBehaviour {
    public string userName;
    public string passwordHash;

    public ClientStatus status = ClientStatus.disconnected;

    public int updateRate = 30;

    private const int MAX_CONNECTIONS = 256;
    private const string IP_ADDR = "81.175.148.247";
    private const string IP_ADDRWS = "81.175.148.247";
    private const int PORT = 8420;
    private const int WEB_PORT = 8421;
    private const int BUFFER_SIZE = 1024;

    public int myHostId;
    public int myConnectionId;
    public int myOwnerId;
    public int[] myResources;
    public ClientRole myRole = ClientRole.user;

    private byte error;

    public Transform player;

    public ClientUI ui;

    public ObjectView ov;
    public CharacterView cv;

    public NetworkChannelArray channelArray;

    private void Connect() {
        GlobalConfig config = new GlobalConfig();
        NetworkTransport.Init(config);

        ConnectionConfig cc = new ConnectionConfig();
        channelArray = new NetworkChannelArray(cc);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTIONS);

        myHostId = NetworkTransport.AddHost(topo, 0);

#if UNITY_WEBGL
        myConnectionId = NetworkTransport.Connect(myHostId, IP_ADDRWS, WEB_PORT, 0, out error);
#else
        myConnectionId = NetworkTransport.Connect(myHostId, IP_ADDR, PORT, 0, out error);
#endif

        status = ClientStatus.connecting;

    }

    private void Receive() {
        int recvHostId, recvConnectionId, recvChannelId, recvBufferSize;
        byte error;

        byte[] buffer = new byte[BUFFER_SIZE];

        while (true) {
            NetworkEventType e = NetworkTransport.Receive(out recvHostId, out recvConnectionId, out recvChannelId, buffer, BUFFER_SIZE, out recvBufferSize, out error);

            if (e == NetworkEventType.Nothing) {
                return;
            }

            switch (e) {
                case NetworkEventType.ConnectEvent: {
                        OnConnect(recvHostId, recvConnectionId, (NetworkError)error);
                        break;
                    }
                case NetworkEventType.DisconnectEvent: {
                        OnDisconnect(recvHostId, recvConnectionId, (NetworkError)error);
                        break;
                    }
                case NetworkEventType.DataEvent: {
                        OnData(recvHostId, recvConnectionId, recvChannelId, buffer, recvBufferSize, (NetworkError)error);
                        break;
                    }
                case NetworkEventType.BroadcastEvent: {
                        OnBroadcast(recvHostId, buffer, recvBufferSize, (NetworkError)error);
                        break;
                    }

                default:
                    ui.Log(0, "Unknown network message type received: " + e);
                    break;
            }
        }
    }

    void OnConnect(int hostId, int connectionId, NetworkError error) {
        ui.Log(100, "Connected to login server");
        status = ClientStatus.login;
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error) {
        ui.Log(100, "Disconnected (", error.ToString(), ")");
        status = ClientStatus.disconnected;
    }

    void OnBroadcast(int hostId, byte[] data, int size, NetworkError error) {
        ui.Log(0, "OnBroadcast(hostId = " + hostId + ", data = "
            + data + ", size = " + size + ", error = " + error.ToString() + ")");
    }

    private void OnData(int hostId, int connectionId, int channelId, byte[] data, int size, NetworkError error) {
        BinaryFormatter bf = new BinaryFormatter();
        Stream stream = new MemoryStream(data);

        for (int i = 0; i < channelArray.channels.Length; i++) {
            if (channelId == channelArray.channels[i].id) {
                switch (channelArray.channels[i].dataType) {
                    case NetworkDataType.ObjectProxyData: {
                            ov.ReceiveObjectProxyData((ObjectProxyData)bf.Deserialize(stream));
                            break;
                        }
                    case NetworkDataType.ObjectProxyTransform: {
                            //ov.ReceiveObjectProxyTransform((ObjectProxyTransform)bf.Deserialize(stream));
                            break;
                        }
                    case NetworkDataType.ServerAuth: {
                            ReceiveServerAuth((ServerAuth)bf.Deserialize(stream));
                            break;
                        }
                    case NetworkDataType.ServerCharacterData: {
                            ReceiveServerCharacterData((ServerCharacterData)bf.Deserialize(stream));
                            break;
                        }
                    case NetworkDataType.ServerBpData: {
                            ReceiveServerBpData((ServerBpData)bf.Deserialize(stream));
                            break;
                        }
                    case NetworkDataType.ServerClientData: {
                            ReceiveServerClientData((ServerClientData)bf.Deserialize(stream));
                            break;
                        }
                }
                break;
            }
        }
    }

    private void ReceiveServerCharacterData(ServerCharacterData serverCharacterData) {
        cv.ReceiveCharacterData(serverCharacterData);
    }

    private void ReceiveServerBpData(ServerBpData serverBpData) {
        ObjectProxy obj = ov.GetObject(serverBpData.id);

        if (obj) {
            Blueprint bp = obj.GetComponent<Blueprint>();
            if (bp) {
                bp.completed = serverBpData.resources;
                bp.op.UpdateState();
                if(bp.op.objectProxyData.owner == myOwnerId) {
                    myResources = serverBpData.resources;
                }
                if(ui.cursor.selBp == bp) {
                    StartCoroutine(ui.blueprintPanel.UpdatePanel(bp));
                }
            }

        }
        //ov.ReceiveServerBpData(serverBpData);
    }

    private void ReceiveServerClientData(ServerClientData serverClientData) {
        ui.UpdateResources(serverClientData.resources);
        myResources = serverClientData.resources;
        myOwnerId = serverClientData.ownerId;
        myRole = serverClientData.role;
    }

    private void ReceiveServerAuth(ServerAuth serverAuth) {
        if (serverAuth.status == ServerAuthStatus.Success) {
            status = ClientStatus.connected;
            ui.Log(50, "Login successful");
        }
        else {
            status = ClientStatus.login;

            if (serverAuth.status == ServerAuthStatus.UserNotFound) {
                ui.Log(50, "User doesn't exist, create account first");
            }
            else if (serverAuth.status == ServerAuthStatus.BadPassword) {
                ui.Log(50, "Invalid passoword. Passwords can't be reset lol");
            }
            else if (serverAuth.status == ServerAuthStatus.UserBanned) {
                ui.Log(0, "You have been banned from the server");
            }
            else if (serverAuth.status == ServerAuthStatus.UserAlreadyExists) {
                ui.Log(50, "Username already exists");
            }
        }
    }

    void Start() {
        ov.InitObjects();
    }

    void Update() {
        if (status == ClientStatus.disconnected) {
            Connect();
        }
        else {
            Receive();
            if (status == ClientStatus.login) {

            }
            else if (status == ClientStatus.connected) {
            }
        }
        ui.statusText.text = status.ToString();
    }

    public void HandleError(byte error) {
        NetworkError msg = (NetworkError)error;
        if (msg != NetworkError.Ok) {
            ui.Log(50, msg.ToString());
        }
    }

    public void SendPlayerPosition() {
        PlayerPosition playerPosition = new PlayerPosition(player.transform);
        byte error = Networking.Send<PlayerPosition>(playerPosition, channelArray.channels[(int)NetworkDataType.PlayerPosition], myHostId, myConnectionId);
        HandleError(error);
    }

    public void SendPlayerCreateObject(ObjectType pt, SrTransform t) {
        PlayerCreateObject playerCreateObject = new PlayerCreateObject(pt, t);
        Networking.Send<PlayerCreateObject>(playerCreateObject, channelArray.channels[(int)NetworkDataType.PlayerCreateObject], myHostId, myConnectionId);
        //ui.Log(0, "Sending Create Object");
    }

    public void SendPlayerCreateObject(ObjectType pt) {
        SrTransform t = new SrTransform(player.transform);
        t.pos = Data.SrVector(player.transform.position + 2f*player.transform.forward, 1000);
        SendPlayerCreateObject(pt, t);
    }

    public void SendPlayerEditObjectTransform(int id, SrTransform t) {
        PlayerEditObject playerEditObject = new PlayerEditObject(id, EditType.Transform, t);
        byte error = Networking.Send<PlayerEditObject>(playerEditObject, channelArray.channels[(int)NetworkDataType.PlayerEditObject], myHostId, myConnectionId);
        //Debug.Log("Sending Edit Object");
        HandleError(error);
    }

    public void SendPlayerEditObject(int id, EditType editType, SrTransform t) {
        PlayerEditObject playerEditObject = new PlayerEditObject(id, editType, t);
        byte error = Networking.Send<PlayerEditObject>(playerEditObject, channelArray.channels[(int)NetworkDataType.PlayerEditObject], myHostId, myConnectionId);
        //Debug.Log("Sending Edit Object");
        HandleError(error);
    }

    public void SendPlayerEditObjectColor(int id, SrTransform t) {
        PlayerEditObject playerEditObject = new PlayerEditObject(id, EditType.Color, t);
        byte error = Networking.Send<PlayerEditObject>(playerEditObject, channelArray.channels[(int)NetworkDataType.PlayerEditObject], myHostId, myConnectionId);
        //Debug.Log("Sending Edit Object");
        HandleError(error);
    }

    public void SendPlayerEditObjectDestroyed(int id, ObjectType pt, SrTransform t) {
        PlayerEditObject playerEditObject = new PlayerEditObject(id, EditType.Destroy, t);
        byte error = Networking.Send<PlayerEditObject>(playerEditObject, channelArray.channels[(int)NetworkDataType.PlayerEditObject], myHostId, myConnectionId);
        //Debug.Log("Sending Edit Object Data : " + pt.ToString());
        HandleError(error);
    }

    public void SendPlayerEditObjectDeposit(int id, SrTransform t, int otherId) {
        PlayerEditObject playerEditObject = new PlayerEditObject(id, EditType.Deposit, t);
        playerEditObject.otherId = otherId;
        byte error = Networking.Send<PlayerEditObject>(playerEditObject, channelArray.channels[(int)NetworkDataType.PlayerEditObject], myHostId, myConnectionId);
        //Debug.Log("Sending deposit " + id.ToString());
        HandleError(error);
    }

    public void SendPlayerTransaction(int trId) {
        int bpId = ui.cursor.selOp.id;
        byte error = Networking.Send<PlayerTransaction>(new PlayerTransaction(trId, bpId, -1), channelArray.channels[(int)NetworkDataType.PlayerTransaction], myHostId, myConnectionId);
        //ui.Log(0, "Sending Edit Object");
        HandleError(error);
        //ObjectProxy op = ov.CreateProxy(ObjectType.CacheCube1, bp.transform.position+Vector3.up*2f, Color.white, bpId);
    }

    public void SendPlayerAuth(bool register = false) {
        PlayerAuth playerAuth = new PlayerAuth(userName, passwordHash, register);
        byte error = Networking.Send<PlayerAuth>(playerAuth, channelArray.channels[(int)NetworkDataType.PlayerAuth], myHostId, myConnectionId);
        status = ClientStatus.connecting;
        ui.Log(100, "Logging in");
        HandleError(error);
    }
}
