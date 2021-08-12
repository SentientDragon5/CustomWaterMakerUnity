using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaterMakerSimple : MonoBehaviour
{
    public WaterSimpleMode mode;
    public int subdivide;
    public List<WaterVertex> vertices = new List<WaterVertex>();
    public List<WaterChunk> chunks = new List<WaterChunk>();
    public List<WaterDelta> deltas = new List<WaterDelta>();
    public Material material;

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            WaterVertex vert = vertices[i];
            vert.Set(WaterVertex.CalcForward(vertices,i), transform);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(vert.position + transform.position, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(vert.positionWorld, vert.rightPoint);
            Gizmos.DrawSphere(vert.rightPoint, 0.1f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(vert.positionWorld, vert.leftPoint);
            Gizmos.DrawSphere(vert.leftPoint, 0.1f);

            Gizmos.DrawLine(vert.positionWorld, vert.down);
            Gizmos.DrawSphere(vert.down, 0.1f);
        }
        
    }
    [ContextMenu("Subdivide")]
    public void Subdivide()
    {
        
    }

    /// <summary>
    /// Destroys all children, then creates custom water meshes:
    /// quads for each straight portion, and polygons for each
    /// junction.
    /// </summary>
    [ContextMenu("Bake")]
    public void Bake()
    {
        chunks.Clear();
        deltas.Clear();
        for (int i = 0; i < vertices.Count; i++)
        {
            WaterVertex vert = vertices[i];
            vert.Set(WaterVertex.CalcForward(vertices, i), transform);



            if (vert.to.Count > 1 || vert.ToFroms(vertices,i).Count > 1)
            {
                List<Vector3> deltaVerts = new List<Vector3>();

                for (int k = 0; k < vert.ToFroms(vertices,i).Count; k++)
                {
                    deltaVerts.Add(vertices[vert.ToFroms(vertices, i)[k]].rightPoint - transform.position);
                    deltaVerts.Add(vertices[vert.ToFroms(vertices, i)[k]].leftPoint - transform.position);
                }
                for (int j = 0; j < vert.to.Count; j++)
                {
                    deltaVerts.Add(vertices[vert.to[j]].rightPoint - transform.position);
                    deltaVerts.Add(vertices[vert.to[j]].leftPoint - transform.position);
                }
                deltas.Add(new WaterDelta(deltaVerts));
            }
            else if (vert.to.Count == 1)
            {
                chunks.Add(new WaterChunk(vert.to[0], i));
            }
        }

        DestroyChildren();

        foreach (WaterChunk chunk in chunks)
        {
            chunk.GenerateMesh(transform, vertices, material);
        }

        for (int i = 1; i < deltas.Count; i++)
        {
            if (deltas[i].vertices.Count == deltas[i - 1].vertices.Count)
            {
                bool isSame = false;
                for (int j = 0; j < deltas[i].vertices.Count; j++)
                {
                    isSame = deltas[i].vertices[j] == deltas[i - 1].vertices[j];
                }
                if (isSame)
                {
                    deltas.RemoveAt(i);
                }
            }
        }
        foreach (WaterDelta delta in deltas)
        {
            delta.GenerateMesh(transform, material);
        }
    }

    public void DestroyChildren()
    {
        int children = transform.childCount;
        for (int i = 0; i < children; i++)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    
}

[System.Serializable]
public class WaterVertex
{
    public Vector3 position;
    public Quaternion rotation;
    public List<int> to = new List<int>();

    public WaterVertex(Vector3 position, Quaternion rotation, Transform parent)
    {
        this.position = position;
        this.parent = parent;
        this.rotation = rotation;
        to = new List<int>();
    }

    public Transform parent;
    public Vector3 positionWorld
    {
        get
        {
            return position + parent.position;
        }
    }
    public List<int> From(List<WaterVertex> all)
    {
        List<int> from = new List<int>();
        foreach (WaterVertex vertex in all)
        {
            if (vertex.to.Contains(this.Index(all)))
            {
                from.Add(vertex.Index(all));
            }
        }
        return from;
    }
    public int Index(List<WaterVertex> all)
    {
        if (all.Contains(this))
        {
            return all.IndexOf(this);
        }
        return -1;
    }
    public List<int> From(List<WaterVertex> all, int i)
    {
        List<int> from = new List<int>();
        for(int j = 0; j < all.Count; j++)
        {
            if (all[j].to.Contains(i))
            {
                from.Add(j);
            }
        }
        return from;
    }
    //SHould return this and any other that shares the same sibling (parent "to" is the same)
    public List<int> ToFroms(List<WaterVertex> all, int i)
    {
        List<int> tofrom = new List<int>();

        for (int k = 0; k < to.Count; k++)
        {
            for (int j = 0; j < all.Count; j++)
            {
                if (all[j].to.Contains(all[i].to[k]))
                {
                    tofrom.Add(j);
                }
            }
        }
        
        return tofrom;
    }

    public void Set(Vector3 forwardDirection, Transform parent)
    {
        float dist = 10f;

        this.parent = parent;
        forwardDir = forwardDirection;

        float flip = 1;

        rd = Vector3.Cross(forwardDir, Vector3.up);

        //If error
        rp = positionWorld + rightDir * dist;
        lp = positionWorld - rightDir * dist;
        dp = positionWorld + Vector3.down * dist;
        area = dist * dist;
        //Debug.Log("r " + rp + " l " + lp + " d " + down + " a " + area + "dist" +dist);


        RaycastHit rhit;
        if (Physics.Raycast(positionWorld, rightDir * flip, out rhit, dist))
        {
            rp = rhit.point;
        }
        RaycastHit lhit;
        if (Physics.Raycast(positionWorld, -1 * rightDir * flip, out lhit, dist))
        {
            lp = lhit.point;
        }
        RaycastHit dhit;
        if (Physics.Raycast(positionWorld, Vector3.down, out dhit, dist))
        {
            dp = dhit.point;
            area = (dhit.distance * rhit.distance) + (dhit.distance * lhit.distance);
        }
        //Debug.Log("r " + rp + " l " + lp + " d " + down + " a " + area);
    }

    Vector3 rd, rp, lp, dp;
    //Raycasts
    public Vector3 forwardDir;

    public Vector3 rightDir
    {
        get
        {

            return rd;
        }
    }
    public Vector3 leftDir
    {
        get
        {
            return -rd;
        }
    }
    public Vector3 rightPoint
    {
        get
        {

            return rp;
        }
    }
    public Vector3 leftPoint
    {
        get
        {
            return lp;
        }
    }
    public Vector3 down
    {
        get { return dp; }
    }
    public float area;


    public static Vector3 CalcForward(List<WaterVertex> all, int i)
    {
        Vector3 fd = Vector3.zero;

        //fd = last.position - next.position / 2f;
        int indexTo = 0;
        //bool flip = false;
        //if (i + indexTo >= all.Count)
        //{
        //    indexTo = i-1;
        //    flip = true;
        //}

        if (all[i].to.Count < 1)
        {
            //Debug.Log(all[i].From(all,i)[0]);
            indexTo = all[i].From(all,i)[0];
        }
        else
        {
            indexTo = all[i].to[0];//i
        }
        //Debug.Log("i: " + i + " ito: " + indexTo + " all count " + all.Count + " to count " + all[i].to.Count);

        fd = all[indexTo].position - all[i].position * (indexTo - i);

        //rotation based
        fd = all[i].rotation * all[i].parent.forward;

        return fd;
    }
}

[System.Serializable]
public class WaterChunk
{
    public int to;
    public int from;
    public float volume;

    public WaterChunk(int to, int from)
    {
        this.to = to;
        this.from = from;
    }

    public void GenerateMesh(Transform parent, List<WaterVertex> all, MeshMaker m)
    {
        all[to].Set(WaterVertex.CalcForward(all, to),parent);
        all[from].Set(WaterVertex.CalcForward(all, from),parent);

        Vector3 offset = parent.position;

        m.verticies.Clear();
        m.verticies.Add(all[to].rightPoint - offset);
        m.verticies.Add(all[to].leftPoint - offset);
        m.verticies.Add(all[from].rightPoint - offset);
        m.verticies.Add(all[from].leftPoint - offset);

        m.triangles.Clear();
        m.triangles = new List<int>() { 0, 1, 2, 1, 3, 2 };//quad
        m.MakeMesh();
    }
    public void GenerateMesh(Transform parent, List<WaterVertex> all, Material material)
    {
        GameObject child = new GameObject("Water Vert");
        child.transform.parent = parent;
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        Mesh m = new Mesh();
        all[to].Set(WaterVertex.CalcForward(all, to), parent);
        all[from].Set(WaterVertex.CalcForward(all, from), parent);

        Vector3 offset = parent.position;
        m.vertices = new Vector3[]
        {
            all[to].rightPoint - offset,
            all[to].leftPoint - offset,
            all[from].rightPoint - offset,
            all[from].leftPoint - offset
        };
        float toHorizontal = Vector3.Distance(all[to].leftPoint, all[to].rightPoint);
        float fromHorizontal = Vector3.Distance(all[from].leftPoint, all[from].rightPoint);
        float vertical = Vector3.Distance(all[to].position, all[from].position);


        m.uv = new Vector2[]
        {
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(0,0),
            new Vector2(1,0)
        };
        m.uv = new Vector2[]
        {
            new Vector2(0,vertical),
            new Vector2(toHorizontal,vertical),
            new Vector2(0,0),
            new Vector2(fromHorizontal,0)
        };

        m.triangles = new int[] { 0, 1, 2, 1, 3, 2 };//quad
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.name = from.ToString() + " to " + to.ToString();
        child.AddComponent<MeshFilter>();
        child.GetComponent<MeshFilter>().mesh = m;
        child.AddComponent<MeshRenderer>();
        child.GetComponent<MeshRenderer>().material = material;
    }
}

[System.Serializable]
public class WaterDelta
{
    public List<Vector3> vertices = new List<Vector3>();
    

    public WaterDelta(List<Vector3> vertices)
    {
        this.vertices = vertices;
    }

    public void GenerateMesh(Transform parent, Material material)
    {
        GameObject child = new GameObject("Water Vert");
        child.transform.parent = parent;
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        Mesh m = Polygon(vertices);
        
        child.AddComponent<MeshFilter>();
        child.GetComponent<MeshFilter>().mesh = m;
        child.AddComponent<MeshRenderer>();
        child.GetComponent<MeshRenderer>().material = material;
    }

    public static Mesh Polygon(List<Vector3> verticies)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Custom " + verticies.Count.ToString() + " sided Polygon";

        verticies = OrderClockwise(verticies, Average(verticies));
        verticies = AverageAdded(verticies);
        mesh.vertices = verticies.ToArray();
        mesh.triangles = Triangles(verticies.Count).ToArray();
        mesh.uv = UV(verticies).ToArray();

        verticies.RemoveAt(0);//remove average

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;


        #region Dependancies
        Vector3 Average(List<Vector3> vector3s)
        {
            int count = 0;
            Vector3 sum = Vector3.zero;
            foreach (Vector3 vert in vector3s)
            {
                sum += vert;
                count++;
            }
            Vector3 average = sum / (float)count;
            return average;
        }
        List<Vector3> AverageAdded(List<Vector3> vector3s)
        {
            List<Vector3> averageFirst = new List<Vector3>();
            averageFirst.Add(Average(vector3s));
            averageFirst.AddRange(vector3s);
            return averageFirst;
        }
        List<Vector3> OrderClockwise(List<Vector3> vector3s, Vector3 center)
        {
            vector3s = vector3s.OrderBy(i => Mathf.Atan2(center.z - i.z, center.x - i.x)).ToList();
            vector3s.Reverse();
            return vector3s;
        }


        List<int> Triangles(int sides)
        {
            //center
            //this
            //next

            List<int> tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                tris.Add(0);
                tris.Add(i);
                if (i + 1 >= sides)
                {
                    tris.Add(1);
                }
                else
                {
                    tris.Add(i + 1);
                }
            }
            return tris;
        }

        List<Vector2> UV(List<Vector3> verticies)
        {
            List<Vector2> uvs = new List<Vector2>();
            foreach (Vector3 vert in verticies)
            {
                uvs.Add(new Vector2(vert.x, vert.z));
            }
            return uvs;
        }
        #endregion
    }
}
