using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public enum NetworkDataType {
    ObjectProxyData,
    ObjectProxyTransform,
    ObjectCompound,
    PlayerAuth,
    PlayerCreateObject,
    PlayerEditObject,
    PlayerTransaction,
    PlayerPosition,
    PlayerQuery,
    ServerClientData,
    ServerCharacterData,
    ServerBpData,
    ServerAuth
}

[System.Serializable]
public enum ServerAuthStatus {
    UserNotFound,
    Success,
    BadPassword,
    UserBanned,
    UserAlreadyExists
}

[System.Serializable]
public class NetworkChannel {
    public NetworkDataType dataType;
    public System.Type daType;
    public int bufferSize;
    public int id;

    public NetworkChannel(NetworkDataType dataType, int bufferSize, int id) {
        this.dataType = dataType;
        this.bufferSize = bufferSize;
        this.id = id;
    }
}

[System.Serializable]
public class NetworkChannelArray {
    public NetworkChannel[] channels;

    public NetworkChannelArray(ConnectionConfig cc) {
        this.channels = new NetworkChannel[(int)NetworkDataType.ServerAuth + 1];
        int i = 0;
        this.channels[i] = new NetworkChannel(NetworkDataType.ObjectProxyData, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ObjectProxyTransform, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ObjectCompound, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerAuth, 1024, cc.AddChannel(QosType.AllCostDelivery));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerCreateObject, 32, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerEditObject, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerTransaction, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerPosition, 1024, cc.AddChannel(QosType.StateUpdate));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.PlayerQuery, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ServerClientData, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ServerCharacterData, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ServerBpData, 1024, cc.AddChannel(QosType.Reliable));
        i++;
        this.channels[i] = new NetworkChannel(NetworkDataType.ServerAuth, 32, cc.AddChannel(QosType.ReliableSequenced));
    }

    public NetworkChannel GetChannel(NetworkDataType dataType) {
        for (int i = 0; i < channels.Length; i++) {
            if (channels[i].dataType == dataType) {
                return channels[i];
            }
        }
        Debug.LogError("No channel for data type " + dataType.ToString());
        return null;
    }
}
public class Networking {
    public static byte Send<T>(T d, NetworkChannel channel, int hostId, int connectionId) {
        byte[] buffer = new byte[1024];
        Stream pack = new MemoryStream(buffer);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(pack, d);

        byte error;
        NetworkTransport.Send(hostId, connectionId, channel.id, buffer, (int)pack.Position, out error);

        if ((NetworkError)error != NetworkError.Ok) {
        }

        return error;
    }
}

[System.Serializable]
public class ObjectProxyData {
    public int id;
    public int owner;
    public int constrainedBy = -1;
    public ObjectType pt;
    public int[] col;
    public SrTransform t;

    public ObjectProxyData(int id, int owner, int constrainedBy, ObjectType pt, SrTransform t, int[] col) {
        this.id = id;
        this.owner = owner;
        this.constrainedBy = constrainedBy;
        this.pt = pt;
        this.t = t;
        this.col = col;
    }
}

[System.Serializable]
public class ObjectProxyTransform {
    public int id;
    public SrTransform t;

    public ObjectProxyTransform(int id, Transform t) {
        this.id = id;
        this.t = new SrTransform(t);
    }
    public ObjectProxyTransform(int id, SrTransform t) {
        this.id = id;
        this.t = t;
    }

    public ObjectProxyTransform(int id, int[] pos) {
        this.id = id;
        this.t = new SrTransform(pos, new int[3] { 0, 0, 0 }, new int[3] { 1000, 1000, 1000 });
    }

    public ObjectProxyTransform(int id) {
        this.id = id;
        this.t = new SrTransform(new int[3] { 0, 0, 0 }, new int[3] { 0, 0, 0 }, new int[3] { 1000, 1000, 1000 });
    }
}

[System.Serializable]
public class ObjectCompound {
    int id;
    public PrimitiveType[] shapes;
    public SrTransform[] ts;

    public ObjectCompound(int id, PrimitiveType[] shapes, SrTransform[] ts) {
        this.id = id;
        this.shapes = shapes;
        this.ts = ts;
    }
}

[System.Serializable]
public class ServerAuth {
    public ServerAuthStatus status;

    public ServerAuth(ServerAuthStatus status) {
        this.status = status;
    }
}

[System.Serializable]
public class ServerBpData {
    public int id;
    public int[] resources;

    public ServerBpData(int id, int[] resources) {
        this.id = id;
        this.resources = resources;
    }
}

[System.Serializable]
public class ServerClientData {
    public int ownerId;
    public ClientRole role;
    public int[] resources;
    public int[] obs;

    public ServerClientData(int ownerId, ClientRole role, int[] resources, int[] obs) {
        this.ownerId = ownerId;
        this.role = role;
        this.resources = resources;
        this.obs = obs;
    }
}

[System.Serializable]
public class PlayerAuth {
    public string user;
    public string passwordHash;
    public bool register;

    public PlayerAuth(string user, string passwordHash, bool register) {
        this.user = user;
        this.passwordHash = passwordHash;
        this.register = register;
    }
}

[System.Serializable]
public class PlayerCreateObject {
    public ObjectType pt;
    public SrTransform t;

    public PlayerCreateObject(ObjectType pt, SrTransform t) {
        this.pt = pt;
        this.t = t;
    }
}

[System.Serializable]
public class ServerCharacterData {
    public int id;
    public SrTransform t;
    public int[] vect = new int[3];
    public string name;

    public ServerCharacterData(int id, SrTransform t, int[] vect, string name) {
        this.id = id;
        this.t = t;
        this.vect = vect;
        this.name = name;
    }
}

[System.Serializable]
public class PlayerQuery {
    public int obId;
    public int clId;

    public PlayerQuery(int obId, int clId) {
        this.obId = obId;
        this.clId = clId;
    }
}

[System.Serializable]
public class PlayerPosition {
    public SrTransform t;

    public PlayerPosition(Transform t) {
        this.t = new SrTransform(t);
    }
}

[System.Serializable]
public class PlayerEditObject {
    public int controlId;
    public EditType editType;
    public SrTransform t;
    public int otherId = -1;

    public PlayerEditObject(int controlId, EditType editType, SrTransform t) {
        this.controlId = controlId;
        this.editType = editType;
        this.t = t;
    }
}

[System.Serializable]
public class PlayerTransaction {
    public int trId;
    public int bpId;
    public int charId;

    public PlayerTransaction(int trId, int bpId, int charId) {
        this.trId = trId;
        this.bpId = bpId;
        this.charId = charId;
    }
}
