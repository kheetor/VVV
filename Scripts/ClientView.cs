using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientView : MonoBehaviour {

    private const int POOLCOUNT = 10;
    private const int POOLSIZE = 13;

    [System.Serializable]
    public class ClientPool {
        public ClientProxy[] clients;
        public float dataAge = 0f;
        public float lastUpdateTime;
        public float maxUpdateTime;

        public ClientPool() {
            this.clients = new ClientProxy[POOLSIZE];
            this.lastUpdateTime = 0f;
            this.maxUpdateTime = 0f;
        }

        public bool HasClients() {
            for (int i = 0; i < clients.Length; i++) {
                if (clients[i]) {
                    return true;
                }
            }
            return false;
        }

        public void UpdateClientsData(ObjectView ov, Server server) {
            Blueprint bp;
            for (int i = 0; i < clients.Length; i++) {
                if (clients[i]) {
                    // remove objects that aren't near anymore
                    for (int o = 0; o < clients[i].nearObjects.Count; o++) {
                        if (clients[i].nearObjects[o] == null || (clients[i].transform.position - clients[i].nearObjects[o].transform.position).sqrMagnitude > 1.2f * clients[i].sqrRange) {
                            clients[i].nearObjects.RemoveAt(o);
                            clients[i].nearObjectStates.RemoveAt(o);
                            o--;
                        }
                    }
                    // remove clients that aren't near anymore
                    for (int o = 0; o < clients[i].nearClients.Count; o++) {
                        if ((clients[i].nearClients[o] == null || (clients[i].transform.position - clients[i].nearClients[o].transform.position).sqrMagnitude > 1.2f * clients[i].sqrRange)) {
                            clients[i].nearClients.RemoveAt(o);
                            clients[i].nearClientStates.RemoveAt(o);
                            o--;
                        }
                    }
                    // add objects that are near
                    for (int o = 0; o < ov.maxIndex; o++) {
                        if(!ov.obs[o] || ov.obs[o].objectProxyData.pt == ObjectType.Destroyed) {
                            continue;
                        }
                        if (((clients[i].transform.position - ov.obs[o].transform.position).sqrMagnitude < clients[i].sqrRange)) {
                            if (!clients[i].nearObjects.Contains(ov.obs[o])) {
                                clients[i].nearObjects.Add(ov.obs[o]);
                                clients[i].nearObjectStates.Add(ov.obs[o].state);

                                if (ov.obs[o].constrainToBp) {
                                    if (!clients[i].nearObjects.Contains(ov.obs[o].constrainToBp.op)) {
                                        clients[i].nearObjects.Add(ov.obs[o].constrainToBp.op);
                                        clients[i].nearObjectStates.Add(-1);
                                        server.SendObjectProxyData(ov.obs[o].constrainToBp.op.objectProxyData, clients[i].hostId, clients[i].connectionId);
                                    }
                                }

                                if (ov.obs[o].objectProxyData.pt != ObjectType.Destroyed) {
                                    //server.SendObjectProxyTransform(objects[o].objectProxyTransform, clients[i].hostId, clients[i].connectionId);
                                    //Debug.Log("Sending data + pos : " + objects[o].objectProxyData.id, objects[o].gameObject);
                                }

                                server.SendObjectProxyData(ov.obs[o].objectProxyData, clients[i].hostId, clients[i].connectionId);
                            }

                            if (ov.obs[o].objectProxyData.pt >= ObjectType.KissaBase && ov.obs[o].objectProxyData.pt <= ObjectType.StaticBase) {
                                bp = ov.obs[o].GetComponent<Blueprint>();
                                if (bp) {
                                    //Debug.Log(bp.gameObject);
                                    server.SendServerBpData(bp.bpData, clients[i].hostId, clients[i].connectionId);
                                }
                                else {
                                    //Debug.Log("not bp " + ov.obs[o].ToString());
                                }
                            }
                        }
                    }
                    // add clients that are near
                    for (int o = 0; o < clients.Length; o++) {
                        if ((clients[o] != null && clients[i] != clients[o] &&
                            (clients[i].transform.position - clients[o].transform.position).sqrMagnitude < clients[i].sqrRange)) {
                            if (!clients[i].nearClients.Contains(clients[o])) {
                                clients[i].nearClients.Add(clients[o]);
                                clients[i].nearClientStates.Add(-1);
                                server.SendServerCharacter(clients[i].hostId, clients[i].connectionId, clients[o].charData);
                                //Debug.Log("Sending char data :" + clients[o].charData.name, clients[o].gameObject);
                            }
                        }
                    }
                }
            }
            dataAge = 0f;
        }
        public float UpdateClientsPos(Server server) {
            float startTime = Time.time * 1000;
            for (int i = 0; i < clients.Length; i++) {
                if (clients[i]) {
                    // check for new transform state for near objects
                    for (int o = 0; o < clients[i].nearObjects.Count; o++) {
                        if (!clients[i].nearObjects[o]) {
                            continue;
                        }
                        if (clients[i].nearObjectStates[o] != clients[i].nearObjects[o].state) {
                            if (clients[i].nearObjects[o].objectProxyData.pt == ObjectType.Destroyed) {
                                server.SendObjectProxyData(clients[i].nearObjects[o].objectProxyData, clients[i].hostId, clients[i].connectionId);
                                clients[i].nearObjects.RemoveAt(o);
                                clients[i].nearObjectStates.RemoveAt(o);
                                o--;
                            }
                            else {
                                server.SendObjectProxyData(clients[i].nearObjects[o].objectProxyData, clients[i].hostId, clients[i].connectionId);
                                clients[i].nearObjectStates[o] = clients[i].nearObjects[o].state;
                            }
                            //Debug.Log("Sending pos : " + clients[i].nearObjects[o].objectProxyData.id, clients[i].nearObjects[o].gameObject);
                        }
                    }
                    for (int o = 0; o < clients[i].nearClients.Count; o++) {
                        if (clients[i].nearClientStates[o] != clients[i].nearClients[o].state) {
                            server.SendServerCharacter(clients[i].hostId, clients[i].connectionId, clients[i].nearClients[o].charData);
                            clients[i].nearClientStates[o] = clients[i].nearClients[o].state;
                            //Debug.Log("Sending char data :" + clients[i].nearClients[o].charData.name, clients[i].nearClients[o].gameObject);
                        }
                    }
                }
            }

            lastUpdateTime = Time.time * 1000 - startTime * 1000;
            maxUpdateTime = Mathf.Max(maxUpdateTime, lastUpdateTime);

            return lastUpdateTime;
        }
    }

    public ObjectView ov;
    public Server server;
    public ClientPool[] clientPools = new ClientPool[POOLCOUNT];
    public int activePool = 0;
    public float dataUpdateInterval = 1f;

    public GameObject proxyTemplate;

    void Start() {

    }
    void FixedUpdate() {
        if (server) {
            UpdateLoop();
        }
    }

    public void UpdateLoop() {
        if (clientPools[activePool].dataAge > dataUpdateInterval) {
            clientPools[activePool].UpdateClientsData(ov, server);
        }
        else {
            float u = clientPools[activePool].UpdateClientsPos(server);
        }

        for (int i = 0; i < clientPools.Length; i++) {
            clientPools[i].dataAge += Time.deltaTime;
        }

        int inc = 1;
        while (!clientPools[(activePool + inc) % POOLCOUNT].HasClients() && inc < POOLCOUNT) {
            inc++;
        }
        activePool = (activePool + inc) % POOLCOUNT;
    }

    public void ReceivePlayerPos(PlayerPosition playerPosition, ClientProxy cp) {
        playerPosition.t.CopyToTransform(cp.transform);
        cp.state = (cp.state + 1) % 10000;
        cp.charData.t = playerPosition.t;
    }

    public ClientProxy AddClientProxy(ClientInfo clientInfo, int clientHostId, int clientConnectionId) {
        GameObject newProxyGO = Instantiate(proxyTemplate);
        newProxyGO.transform.SetParent(transform);
        newProxyGO.transform.position = Data.DesrVector(clientInfo.lastPos, 1000);
        ClientProxy newProxy = newProxyGO.AddComponent<ClientProxy>();
        newProxy.hostId = clientHostId;
        newProxy.connectionId = clientConnectionId;
        newProxy.info = clientInfo;
        newProxy.charData = new ServerCharacterData(clientInfo.id, new SrTransform(newProxy.transform), new int[] { 0, 0, 0 }, clientInfo.name);
        FillPool(newProxy);
        return newProxy;
    }

    public void FillPool(ClientProxy cp) {
        for (int i = 0; i < clientPools.Length; i++) {
            for (int j = 0; j < clientPools[i].clients.Length; j++) {
                if (!clientPools[i].clients[j]) {
                    clientPools[i].clients[j] = cp;
                    return;
                }
            }
        }
        Debug.LogError("Clientpool overflow error");
    }

    public ClientProxy GetClientProxy(string name) {
        for (int i = 0; i < clientPools.Length; i++) {
            for (int j = 0; j < clientPools[i].clients.Length; j++) {
                if (clientPools[i].clients[j] && clientPools[i].clients[j].name == name) {
                    return clientPools[i].clients[j];
                }
            }
        }
        return null;
    }

    public ClientProxy GetClientProxy(int id) {
        for (int i = 0; i < clientPools.Length; i++) {
            for (int j = 0; j < clientPools[i].clients.Length; j++) {
                if (clientPools[i].clients[j] && clientPools[i].clients[j].info.id == id) {
                    return clientPools[i].clients[j];
                }
            }
        }
        return null;
    }

    public ClientProxy GetClientProxy(int hostId, int connectionId) {
        for (int i = 0; i < clientPools.Length; i++) {
            for (int j = 0; j < clientPools[i].clients.Length; j++) {
                if (clientPools[i].clients[j] && clientPools[i].clients[j].connectionId == connectionId && clientPools[i].clients[j].hostId == hostId) {
                    return clientPools[i].clients[j];
                }
            }
        }
        return null;
    }
}
