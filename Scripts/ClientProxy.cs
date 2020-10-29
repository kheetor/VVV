using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientProxy : MonoBehaviour {

    public int hostId;
    public int connectionId;
    public ClientInfo info;
    //public int id;
    //public int[] resources = new int[8];
    //public ClientRole role = ClientRole.user;
    //public List<int> obs = new List<int>();
    public ServerCharacterData charData;
    public int state;

    public List<ClientProxy> nearClients = new List<ClientProxy>();
    public List<int> nearClientStates = new List<int>();

    public List<ObjectProxy> nearObjects = new List<ObjectProxy>();
    public List<int> nearObjectStates = new List<int>();

    public float sqrRange = 10000f;
}
