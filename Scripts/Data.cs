using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EditType {
    Transform,
    Deposit,
    Color,
    Shape,
    Carry,
    Destroy
}

public enum Resources {
    organic,
    mineral,
    metal,
    tech,
    power,
    credits
}

public enum ObjectType {
    CacheCube1, CacheSphere1, CacheCylinder1, CacheTri1,
    CacheCube2, CacheSphere2, CacheCylinder2, CacheTri2,
    CacheCube3, CacheSphere3, CacheCylinder3, CacheTri3,
    CacheCube4, CacheSphere4, CacheCylinder4, CacheTri4,
    CacheCube5, CacheSphere5, CacheCylinder5, CacheTri5,
    NaturalA1, NaturalB1, NaturalC1,
    NaturalA2, NaturalB2, NaturalC2,
    NaturalA3, NaturalB3, NaturalC3,
    NaturalA4, NaturalB4, NaturalC4,
    NaturalA5, NaturalB5, NaturalC5,
    KissaBase, PlayerBase, Plant3, Plant4, StaticBase,
    DroneFactory, Building2, Building3, Building4, Building5,
    Conveyor,
    Destroyed
}

[System.Serializable]
public struct SrTransform {
    public int[] pos;
    public int[] rot;
    public int[] scale;

    public SrTransform(int[] pos, int[] rot, int[] scale) {
        this.pos = pos;
        this.rot = rot;
        this.scale = scale;
    }

    public SrTransform(int[] pos) {
        this.pos = pos;
        this.rot = new int[0];
        this.scale = new int[0];
    }

    public SrTransform(Transform t) {
        this.pos = Data.SrVector(t.position, 1000f);
        this.rot = Data.SrVectorAngles(t.eulerAngles);
        this.scale = Data.SrVector(t.localScale, 100f);
    }

    public void CopyToTransform(Transform t) {
        t.position = Data.DesrVector(pos, 1000f);
        t.eulerAngles = Data.DesrVector(rot, 720f);
        t.localScale = Data.DesrVector(scale, 100f);
    }
}

public class Data {

    public static Vector3 AbsVector(Vector3 v) {
        return new Vector3(
            Mathf.Abs(v.x),
            Mathf.Abs(v.y),
            Mathf.Abs(v.z)
        );
    }

    public static Vector3 ClampVector(Vector3 v, float cl = 0.2f, float ch = 20f) {
        return new Vector3(
            Mathf.Clamp(v.x, cl, ch),
            Mathf.Clamp(v.y, cl, ch),
            Mathf.Clamp(v.z, cl, ch)
            );
    }

    public static Vector3 RoundVector(Vector3 v, float precision = 0.5f) {
        float frac = 1f / precision;
        return new Vector3(
            Mathf.Round(v.x * frac) * precision,
            Mathf.Round(v.y * frac) * precision,
            Mathf.Round(v.z * frac) * precision
            );
    }

    public static bool GetBasePos(Vector3 pos, out Vector3 validPos) {
        validPos = pos;
        if ((Mathf.Abs(pos.x) < 100f && Mathf.Abs(pos.z) < 100f) || Mathf.Abs(pos.x) > 1000f || Mathf.Abs(pos.z) > 1000f) {
            return false;
        }
        validPos = Vector3.one * 16 + RoundVector(pos - Vector3.one * 16, 32f);
        validPos = new Vector3(validPos.x, pos.y, validPos.z);
        return true;
    }

    public static int[] ResourceAdd(int[] target, int[] addition) {
        for(int i = 0; i < Mathf.Min(target.Length, addition.Length); i++) {
            if (i >= 6) {
                target[i] = Mathf.Max(target[i], addition[i]);
            }
            else {
                target[i] += addition[i];
            }
        }

        return target;
    }

    public static int[] ResourceSub(int[] target, int[] subtraction) {
        for (int i = 0; i < Mathf.Min(6, target.Length, subtraction.Length); i++) {
            target[i] -= subtraction[i];
        }
        return target;
    }

    public static Color DesrColor(int[] values) {
        return Color.HSVToRGB(((float)values[0]) / 100f, ((float)values[1]) / 100f, ((float)values[2]) / 100f);
    }

    public static int[] SrColor(Color col) {
        float h, s, v;
        Color.RGBToHSV(col, out h, out s, out v);
        return new int[] {
            Mathf.RoundToInt(h * 100f),
            Mathf.RoundToInt(s * 100f),
            Mathf.RoundToInt(v * 100f)
        };
    }

    public static int GetObjectResourceType(ObjectType pt) {
        int i = (int)pt;
        if (i < 20) {
            return (i / 4);
        }
        else if(i < 40) {
            return (i - 20) / 3;
        }
        else {
            Debug.LogError("Invalid resource to deposit");
            return 0;
        }
    }

    [System.Serializable]
    public struct OldSaveData {
        public OldSrObject[] SrObs;
        public ClientInfo[] SrClients;

        public OldSaveData(OldSrObject[] obs, ClientInfo[] SrClients) {
            this.SrObs = obs;
            this.SrClients = SrClients;
        }
    }

    [System.Serializable]
    public struct SaveData {
        public ObjectProxyData[] obs;
        public ClientInfo[] SrClients;

        public SaveData(ObjectProxyData[] obs, ClientInfo[] SrClients) {
            this.obs = obs;
            this.SrClients = SrClients;
        }
    }

    [System.Serializable]
    public struct SrClientInfo {
        public int id;
        public string name;
        public string passwordHash;
        public int[] resources;
        public ClientRole role;
        public int[] lastPos;

        public SrClientInfo (int id, string name, string passwordHash, int[] res, ClientRole role, int[] lastPos) {
            this.id = id;
            this.name = name;
            this.passwordHash = passwordHash;
            this.resources = res;
            this.role = role;
            this.lastPos = lastPos;
        }
    }

    [System.Serializable]
    public struct OldSrObject {
        public ObjectProxyData data;
        public ObjectProxyTransform t;

        public OldSrObject(ObjectProxyData data, ObjectProxyTransform t) {
            this.data = data;
            this.t = t;
        }
    }

    public static int[] SrVector(Vector3 v, float clamp, int precision = 1000) {
        return new int[] {
            Mathf.RoundToInt(Mathf.Clamp(v.x,-clamp, clamp) * precision),
            Mathf.RoundToInt(Mathf.Clamp(v.y,-clamp, clamp) * precision),
            Mathf.RoundToInt(Mathf.Clamp(v.z,-clamp, clamp) * precision)
        };
    }

    public static int[] SrVectorAngles(Vector3 v, int precision = 1000) {
        return new int[] {
            Mathf.RoundToInt(((v.x+360f)%360) * precision),
            Mathf.RoundToInt(((v.y+360f)%360) * precision),
            Mathf.RoundToInt(((v.z+360f)%360) * precision)
        };
    }

    public static Vector3 DesrVector(int[] srVector, float clamp, int precision = 1000) {
        return new Vector3(
            Mathf.Clamp((float)srVector[0] / precision, -clamp, clamp),
            Mathf.Clamp((float)srVector[1] / precision, -clamp, clamp),
            Mathf.Clamp((float)srVector[2] / precision, -clamp, clamp)
        );
    }
}

