using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Jobs;

public enum ClientRole {
    owner,
    admin,
    user,
    banned
}

[System.Serializable]
public class ClientInfo {
    public int id;
    public string name;
    public string passwordHash;
    public int[] resources;
    public List<int> obs;
    public ClientRole role;
    public int[] lastPos;

    public ClientInfo(int id, string name, string passwordHash, int[] resources, List<int> obs, int[] lastPos) {
        this.id = id;
        this.name = name;
        this.passwordHash = passwordHash;
        this.resources = resources;
        this.obs = obs;
        this.role = ClientRole.user;
        this.lastPos = lastPos;
    }
}

public class Server : MonoBehaviour {

    private const int MAX_CONNECTIONS = 256;
    private const string IP_ADDR = "127.0.0.1";
    private const int PORT = 8420;
    private const int WEB_PORT = 8421;
    private const int BUFFER_SIZE = 1024;
    int hostId;
    int webHostId;

    NetworkChannelArray channelArray;

    private bool isInit = false;

    public ObjectView ov;
    public ClientView cv;

    public List<int> pendAuthClients = new List<int>();
    public List<ClientInfo> clients = new List<ClientInfo>();

    private float saveTimer = 0f;
    private float saveInterval = 600f;

    void Start() {
        Init();
    }

    private void Init() {
        GlobalConfig config = new GlobalConfig();
        config.MaxHosts = 128;
        NetworkTransport.Init(config);

        ConnectionConfig cc = new ConnectionConfig();
        channelArray = new NetworkChannelArray(cc);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTIONS);

        hostId = NetworkTransport.AddHost(topo, PORT);
        webHostId = NetworkTransport.AddWebsocketHost(topo, WEB_PORT);

        ov.InitObjects();
        LoadState();

        isInit = true;
    }

    void Update() {
        if (!isInit) {
            return;
        }

        Receive();

        if (saveTimer > saveInterval || Input.GetKeyDown(KeyCode.F10)) {
            SaveState();
            saveTimer = 0f;
        }
        saveTimer += Time.deltaTime;
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
                    Debug.LogError("Unknown network message type received: " + e);
                    break;
            }
        }
    }

    void OnConnect(int hostId, int connectionId, NetworkError error) {
        pendAuthClients.Add(connectionId);
        Debug.Log("OnConnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
    }

    void OnDisconnect(int hostId, int connectionId, NetworkError error) {
        Debug.Log("OnDisconnect(hostId = " + hostId + ", connectionId = "
            + connectionId + ", error = " + error.ToString() + ")");
        byte dcError;
        NetworkTransport.Disconnect(hostId, connectionId, out dcError);
        if (DisconnectClient(cv.GetClientProxy(hostId, connectionId))) {
            Debug.Log("User disconnected in response");
        }
    }

    void OnBroadcast(int hostId, byte[] data, int size, NetworkError error) {
        Debug.Log("OnBroadcast(hostId = " + hostId + ", data = "
            + data + ", size = " + size + ", error = " + error.ToString() + ")");
    }

    private void OnData(int hostId, int connectionId, int channelId, byte[] data, int size, NetworkError error) {
        ClientProxy cp = cv.GetClientProxy(hostId, connectionId);
        BinaryFormatter bf = new BinaryFormatter();
        Stream stream = new MemoryStream(data);

        for (int i = 0; i < channelArray.channels.Length; i++) {
            if (channelId == channelArray.channels[i].id) {
                switch (channelArray.channels[i].dataType) {
                    case NetworkDataType.PlayerAuth: {
                            StartCoroutine(ProcessAuthCoroutine((PlayerAuth)bf.Deserialize(stream), hostId, connectionId));
                            break;
                        }
                    case NetworkDataType.PlayerPosition: {
                            OnPlayerPosition((PlayerPosition)bf.Deserialize(stream), cp);
                            break;
                        }
                    case NetworkDataType.PlayerCreateObject: {
                            OnPlayerCreateObject((PlayerCreateObject)bf.Deserialize(stream), cp);
                            break;
                        }
                    case NetworkDataType.PlayerQuery: {
                            OnPlayerQuery((PlayerQuery)bf.Deserialize(stream), cp);
                            break;
                        }
                    case NetworkDataType.PlayerTransaction: {
                            OnPlayerTransaction((PlayerTransaction)bf.Deserialize(stream), cp);
                            break;
                        }
                    case NetworkDataType.PlayerEditObject: {
                            OnPlayerEditObject((PlayerEditObject)bf.Deserialize(stream), cp);
                            break;
                        }
                }
                break;
            }
        }
    }

    private IEnumerator ProcessAuthCoroutine(PlayerAuth auth, int clientHostId, int clientConnectionId) {
        if (auth.register) {
            Debug.Log("New player registering : " + auth.user);
        }
        else {
            Debug.Log("New player joining : " + auth.user);
        }
        yield return new WaitForSeconds(2f);
        OnPlayerAuth(auth, clientHostId, clientConnectionId);
    }

    private void OnPlayerAuth(PlayerAuth auth, int clientHostId, int clientConnectionId) {
        if (auth.register) {
            for (int i = 0; i < clients.Count; i++) {
                if (clients[i].name == auth.user) {
                    SendServerAuth(clientHostId, clientConnectionId, ServerAuthStatus.UserAlreadyExists);
                    Debug.Log("already exists");
                    return;
                }
            }
            ClientInfo registerClient = new ClientInfo(clients.Count, auth.user, auth.passwordHash, new int[] { 30, 30, 30, 30, 30, 100000, 0, 0 }, new List<int>(), new int[3]);
            clients.Add(registerClient);
            AuthClient(registerClient, clientHostId, clientConnectionId);
            return;
        }
        else {
            for (int i = 0; i < clients.Count; i++) {
                if (clients[i].name == auth.user) {
                    if (auth.passwordHash == clients[i].passwordHash) {
                        if (clients[i].role == ClientRole.banned) {
                            SendServerAuth(clientHostId, clientConnectionId, ServerAuthStatus.UserBanned);
                            Debug.Log("banned");
                            return;
                        }
                        DisconnectClient(cv.GetClientProxy(auth.user));
                        AuthClient(clients[i], clientHostId, clientConnectionId);
                        return;
                    }
                    else {
                        SendServerAuth(clientHostId, clientConnectionId, ServerAuthStatus.BadPassword);
                        Debug.Log("bad password");
                        return;
                    }
                }
            }
            SendServerAuth(clientHostId, clientConnectionId, ServerAuthStatus.UserNotFound);
            Debug.Log("user not found");
            return;
        }
    }

    private void OnPlayerTransaction(PlayerTransaction transaction, ClientProxy cp) {
        ObjectProxy op = ov.GetObject(transaction.bpId);
        if (op) {
            Blueprint bp = op.GetComponent<Blueprint>();
            if (bp && bp.transacts.Count > transaction.trId) {
                if (bp.transacts[transaction.trId].CanTransact(cp.info.resources)) {
                    cp.info.resources = Data.ResourceSub(cp.info.resources, bp.transacts[transaction.trId].price);
                    cp.info.resources = Data.ResourceAdd(cp.info.resources, bp.transacts[transaction.trId].reward);
                }
            }
        }
        SendServerClientData(cp);
    }

    private void AuthClient(ClientInfo newClientInfo, int clientHostId, int clientConnectionId) {
        ClientProxy cp = cv.AddClientProxy(newClientInfo, clientHostId, clientConnectionId);
        pendAuthClients.Remove(clientConnectionId);

        SendServerAuth(clientHostId, clientConnectionId, ServerAuthStatus.Success);
        SendServerClientData(cp);
    }

    public bool DisconnectClient(ClientProxy cp) {
        if (!cp) {
            return false;
        }
        Destroy(cp.gameObject);
        byte error;
        NetworkTransport.Disconnect(cp.hostId, cp.connectionId, out error);
        return true;
    }

    public void SendServerAuth(int clientHostId, int clientConnectionId, ServerAuthStatus status) {
        Networking.Send<ServerAuth>(new ServerAuth(status), channelArray.channels[(int)NetworkDataType.ServerAuth], clientHostId, clientConnectionId);
    }
    public void SendServerClientData(ClientProxy cp) {
        Networking.Send<ServerClientData>(new ServerClientData(cp.info.id, cp.info.role, cp.info.resources, cp.info.obs.ToArray()), channelArray.channels[(int)NetworkDataType.ServerClientData], cp.hostId, cp.connectionId);
        Debug.Log("sent client data");
    }

    public void SendServerCharacter(int clientHostId, int clientConnectionId, ServerCharacterData cd) {
        Networking.Send<ServerCharacterData>(cd, channelArray.channels[(int)NetworkDataType.ServerCharacterData], clientHostId, clientConnectionId);
        Debug.Log("sent character data");
    }

    public void SendObjectProxyTransform(ObjectProxyTransform objectProxyTransform, int clientHostId, int clientConnectionId) {
        Networking.Send<ObjectProxyTransform>(objectProxyTransform, channelArray.channels[(int)NetworkDataType.ObjectProxyTransform], clientHostId, clientConnectionId);
        //Debug.Log("Sending pos : " + objectProxyTransform.id);
    }

    public void SendObjectProxyDestroyed(int id, ClientProxy cp) {
        SendObjectProxyData(new ObjectProxyData(id, -1, -1, ObjectType.Destroyed, new SrTransform(), new int[3]), cp.hostId, cp.connectionId);
    }

    public void SendObjectProxyData(ObjectProxyData objectProxyData, int clientHostId, int clientConnectionId) {
        Networking.Send<ObjectProxyData>(objectProxyData, channelArray.channels[(int)NetworkDataType.ObjectProxyData], clientHostId, clientConnectionId);
        //Debug.Log("Send data : " + objectProxyData.id.ToString());
    }

    public void SendServerBpData(ServerBpData serverBpData, int clientHostId, int clientConnectionId) {
        Networking.Send<ServerBpData>(serverBpData, channelArray.channels[(int)NetworkDataType.ServerBpData], clientHostId, clientConnectionId);
        //Debug.Log("Send data : " + objectProxyData.id.ToString());
    }

    public void SendObjectDestroyed(int id, int clientHostId, int clientConnectionId) {
        Networking.Send<ObjectProxyData>(new ObjectProxyData(id, -1, -1, ObjectType.Destroyed, new SrTransform(), new int[0]), channelArray.channels[(int)NetworkDataType.ObjectProxyData], clientHostId, clientConnectionId);
    }

    public void OnPlayerPosition(PlayerPosition playerPosition, ClientProxy cp) {
        cv.ReceivePlayerPos(playerPosition, cp);
    }

    public void OnPlayerQuery(PlayerQuery q, ClientProxy cp) {
        if (q.obId >= 0) {
            ObjectProxy op = ov.GetObject(q.obId);
            if (op) {
                SendObjectProxyData(op.objectProxyData, cp.hostId, cp.connectionId);
            }
            else {
                SendObjectProxyDestroyed(q.obId, cp);
            }
        }
        else if (q.clId >= 0) {
            ClientProxy cpOther = cv.GetClientProxy(q.clId);
            if (cpOther) {
                SendServerCharacter(cp.hostId, cp.connectionId, cpOther.charData);
            }
            else {
                SendServerCharacter(cp.hostId, cp.connectionId, new ServerCharacterData(q.clId, new SrTransform(), new int[3], ""));
            }
        }
    }

    public int GetCost(int[] size) {
        return 1000 + (size[0] / 100) * (size[1] / 100) * (size[2] / 100);
    }

    public void OnPlayerCreateObject(PlayerCreateObject playerCreateObject, ClientProxy cp) {
        Vector3 pos = Data.DesrVector(playerCreateObject.t.pos, 1000);
        //Debug.Log("Create object to pos " + pos.ToString());
        if (playerCreateObject.pt == ObjectType.PlayerBase) {
            if (cp.info.resources[5] > 10000 || cp.info.resources[7] > 0) {
                cp.info.resources[5] -= 10000;
                cp.info.resources[7] = 1;
            }
            else {
                return;
            }
        }

        //default appearance
        ObjectProxy op = ov.CreateProxy(playerCreateObject.pt, pos, Color.white, -1);

        op.objectProxyData = new ObjectProxyData(op.id, cp.info.id, -1, playerCreateObject.pt, new SrTransform(op.transform), new int[] { 0, 100, 0 });
        cp.info.obs.Add(op.id);

        if (playerCreateObject.pt == ObjectType.PlayerBase) {
            op.GetComponent<Blueprint>().completed = cp.info.resources;
        }

        SendObjectProxyData(op.objectProxyData, cp.hostId, cp.connectionId);
        SendServerClientData(cp);
    }

    public int Sign(int i) {
        return i >= 0 ? 1 : -1;
    }

    public SrTransform ClampScale(SrTransform t, int cl, int ch) {
        return new SrTransform(t.pos, t.rot, new int[] {
            Mathf.Clamp(Mathf.Abs(t.scale[0]),cl, ch) * Sign(t.scale[0]),
            Mathf.Clamp(Mathf.Abs(t.scale[1]),cl, ch) * Sign(t.scale[1]),
            Mathf.Clamp(Mathf.Abs(t.scale[2]),cl, ch) * Sign(t.scale[2])
        });
    }

    public void SaveState() {
        ObjectProxyData[] obs = new ObjectProxyData[ov.maxIndex];
        for (int i = 0; i < ov.maxIndex; i++) {
            if (ov.obs[i]) {
                obs[i] = ov.obs[i].objectProxyData;
            }
        }

        Data.SaveData saveData = new Data.SaveData(obs, clients.ToArray());

        string jsonData = JsonUtility.ToJson(saveData, true);

        System.IO.File.WriteAllText(Application.persistentDataPath + "/VVVserverSave.json", jsonData);

        Debug.Log("Wrote " + Application.persistentDataPath + "/VVVserverSave.json");

    }

    public void LoadStateOld() {
        Data.OldSaveData saveData = JsonUtility.FromJson<Data.OldSaveData>(File.ReadAllText(Application.persistentDataPath + "/VVVserverSave.json"));

        ObjectProxy[] loaded = new ObjectProxy[saveData.SrObs.Length];

        // Instantiate
        for (int i = 0; i < saveData.SrObs.Length; i++) {
            int pt = (int)saveData.SrObs[i].data.pt;
            if (!ov.obs[i] && saveData.SrObs[i].data.col.Length > 0 && pt != (int)ObjectType.Destroyed) {
                ObjectProxy op = ov.CreateProxy(saveData.SrObs[i].data.pt, Vector3.zero, Data.DesrColor(saveData.SrObs[i].data.col), saveData.SrObs[i].data.constrainedBy, saveData.SrObs[i].data.id);

                loaded[i] = op;
            }
        }

        // Place correctly in obs array
        for (int i = 0; i < loaded.Length; i++) {
            if (loaded[i]) {
                ov.obs[i] = loaded[i];
                ov.maxIndex = Mathf.Max(ov.maxIndex, i);
            }
        }

        // Update data and transform
        for (int i = 0; i < saveData.SrObs.Length; i++) {
            if (ov.obs[i] && saveData.SrObs[i].data.col.Length > 0 && saveData.SrObs[i].data.pt != ObjectType.Destroyed) {
                ov.obs[i].id = saveData.SrObs[i].data.id;
                ov.obs[i].objectProxyData = saveData.SrObs[i].data;
                ov.TransformObject(ov.obs[i], saveData.SrObs[i].t.t);
                ov.obs[i].objectProxyData.t = saveData.SrObs[i].t.t;

                /*
                Debug.Log(
                    i.ToString() + ":" +
                    ov.obs[i].objectProxyData.pt.ToString() + " pos at " +
                    ov.obs[i].transform.position.ToString() + 
                    " - " + 
                    ov.obs[i].objectProxyTransform.t.pos[0].ToString() + "," +
                    ov.obs[i].objectProxyTransform.t.pos[1].ToString() + "," +
                    ov.obs[i].objectProxyTransform.t.pos[2].ToString()
                    );
                    */

            }
        }

        // Clients
        clients.Clear();
        for (int i = 0; i < saveData.SrClients.Length; i++) {
            clients.Add(saveData.SrClients[i]);
        }
    }

    public void LoadState() {
        Data.SaveData saveData = JsonUtility.FromJson<Data.SaveData>(File.ReadAllText(Application.persistentDataPath + "/VVVserverSave.json"));

        ObjectProxy[] loaded = new ObjectProxy[saveData.obs.Length];

        // Instantiate
        for (int i = 0; i < saveData.obs.Length; i++) {
            int pt = (int)saveData.obs[i].pt;
            if (!ov.obs[i] && saveData.obs[i].col.Length > 0 && pt != (int)ObjectType.Destroyed) {
                ObjectProxy op = ov.CreateProxy(saveData.obs[i].pt, Vector3.zero, Data.DesrColor(saveData.obs[i].col), saveData.obs[i].constrainedBy, saveData.obs[i].id);

                loaded[i] = op;
            }
        }

        // Place correctly in obs array
        for (int i = 0; i < loaded.Length; i++) {
            if (loaded[i]) {
                ov.obs[i] = loaded[i];
                ov.maxIndex = Mathf.Max(ov.maxIndex, i);
            }
        }

        // Update data and transform
        for (int i = 0; i < saveData.obs.Length; i++) {
            if (ov.obs[i] && saveData.obs[i].col.Length > 0 && saveData.obs[i].pt != ObjectType.Destroyed) {
                ov.obs[i].id = saveData.obs[i].id;
                ov.obs[i].objectProxyData = saveData.obs[i];
                saveData.obs[i].t.CopyToTransform(ov.obs[i].transform);

                /*
                Debug.Log(
                    i.ToString() + ":" +
                    ov.obs[i].objectProxyData.pt.ToString() + " pos at " +
                    ov.obs[i].transform.position.ToString() + 
                    " - " + 
                    ov.obs[i].objectProxyTransform.t.pos[0].ToString() + "," +
                    ov.obs[i].objectProxyTransform.t.pos[1].ToString() + "," +
                    ov.obs[i].objectProxyTransform.t.pos[2].ToString()
                    );
                    */

            }
        }

        // Clients
        clients.Clear();
        for (int i = 0; i < saveData.SrClients.Length; i++) {
            clients.Add(saveData.SrClients[i]);
        }
    }

    public void OnPlayerEditObject(PlayerEditObject playerEditObject, ClientProxy cp) {
        //Debug.Log(cp.id.ToString() + " edits " + playerEditObject.controlId.ToString() + " : " + playerEditObject.editType.ToString());
        ObjectProxy obj = ov.GetObject(playerEditObject.controlId);

        if (!obj) {
            SendObjectProxyDestroyed(playerEditObject.controlId, cp);
        }

        if (cp.info.role != ClientRole.admin && obj.objectProxyData.owner >= 0 && obj.objectProxyData.owner != cp.info.id) {
            return;
        }

        if ((int)obj.objectProxyData.pt >= (int)ObjectType.NaturalA1 && (int)obj.objectProxyData.pt <= (int)ObjectType.NaturalC5 && playerEditObject.editType != EditType.Carry) {
            obj.objectProxyData.owner = -1;
        }

        //playerEditObject.t = ClampScale(playerEditObject.t, 200, 20000);

        ov.ReceiveObjectEdit(playerEditObject, cp);

        if (playerEditObject.editType == EditType.Transform) {
            SendObjectProxyData(obj.objectProxyData, cp.hostId, cp.connectionId);
        }
        else if (playerEditObject.editType == EditType.Deposit) {
            ObjectProxy op = ov.GetObject(playerEditObject.otherId);
            if (op) {
                Blueprint bp = op.GetComponent<Blueprint>();
                if (bp) {
                    if (op.objectProxyData.pt == ObjectType.KissaBase) {
                        cp.info.resources[5] += 100;
                    }
                    else if (op.objectProxyData.owner != cp.info.id) {
                        ClientProxy bpCp = cv.GetClientProxy(op.objectProxyData.owner);
                        if (bpCp) {
                            bpCp.info.resources = bp.completed;
                            SendServerClientData(bpCp);
                        }
                        else {
                            for (int i = 0; i < clients.Count; i++) {
                                if (clients[i].id == op.objectProxyData.owner) {
                                    clients[i].resources = bp.completed;
                                }
                            }
                        }
                    }
                }
            }
            SendObjectProxyData(obj.objectProxyData, cp.hostId, cp.connectionId);
        }
        else if (playerEditObject.editType == EditType.Color) {
            SendObjectProxyData(obj.objectProxyData, cp.hostId, cp.connectionId);
        }
        else if (playerEditObject.editType == EditType.Destroy) {
            if (cp.info.obs.Contains(playerEditObject.controlId)) {
                cp.info.obs.Remove(playerEditObject.controlId);
            }
            SendObjectProxyDestroyed(playerEditObject.controlId, cp);
        }
        SendServerClientData(cp);
    }
}
