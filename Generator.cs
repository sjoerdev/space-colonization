using System.Collections.Generic;
using UnityEngine;

public class Passage
{
    public int id;
    public int verticesid;
    public Vector3 start;
    public Vector3 end;
    public Vector3 direction;
    public Passage parent;
    public List<Passage> children = new List<Passage>();
    public List<Vector3> attractors = new List<Vector3>();
    
    public Passage(Vector3 start, Vector3 end, Vector3 direction, Passage parent = null)
    {
        this.start = start;
        this.end = end;
        this.direction = direction;
        this.parent = parent;
    }
}

public class Generator : MonoBehaviour
{
    [Header("Space Colonization Settings")]
    public int initialNodeAmount = 1000;
    public int nodesLeft = 0;
    public float passageLength = 0.1f;
    public float attractionRange = 1;
    public float killRange = 0.6f;
    public float randomGrowth = 0.2f;

    [Header("Cave Shape Settings")]
    public Transform ellipsoidTransform;
    public float surfaceHeight = 2;

    [Header("Mesh Generation Settings")]
    public Material caveMaterial;
    [Range(0.005f, 0.1f)] public float passageWidth = 0.01f;
    [Range(3, 8)] public int subdivisions = 6;

    [Header("Camera & Player Settings")]
    [Range(1, 10)] public float cameraOffset = 3;
    [Range(0.4f, 2)] public float cameraSpeed = 1;
    [Range(0.1f, 1)] public float walkInterval = 0.5f;
    public int othersAmount = 12;

    private List<Vector3> nodes = new List<Vector3>();
    private List<int> activeNodes = new List<int>();
    private Passage firstPassage;
    private List<Passage> passages = new List<Passage>();
    private List<Passage> extremities = new List<Passage>();
    private bool finished = false;
    private int step = 0;
    private Passage playerEntrance;
    private Passage playerPassage;
    private List<Passage> othersEntrances = new List<Passage>();
    private List<Passage> othersPassages = new List<Passage>();

    private void OnDrawGizmos()
    {
        // draw passages
        if (passages.Count > 0 && !finished)
        {
            for (int i = 0; i < passages.Count; i++)
            {
                bool extremity = extremities.Contains(passages[i]);
                Gizmos.color = extremity ? Color.green : Color.white;
                Gizmos.DrawLine(passages[i].start, passages[i].end);
            }
        }

        // draw shape
        if (ellipsoidTransform != null)
        {
            // Visualize the ellipsoid
            Gizmos.color = Color.blue;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = ellipsoidTransform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            Gizmos.matrix = oldMatrix;

            // Visualize the surfaceHeight
            var size = 2;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-size, surfaceHeight, 0), new Vector3(size, surfaceHeight, 0));
            Gizmos.DrawLine(new Vector3(0, surfaceHeight, -size), new Vector3(0, surfaceHeight, size));
            Gizmos.DrawLine(new Vector3(-size, surfaceHeight, 0), new Vector3(0, surfaceHeight, -size));
            Gizmos.DrawLine(new Vector3(size, surfaceHeight, 0), new Vector3(0, surfaceHeight, size));
            Gizmos.DrawLine(new Vector3(0, surfaceHeight, -size), new Vector3(size, surfaceHeight, 0));
            Gizmos.DrawLine(new Vector3(-size, surfaceHeight, 0), new Vector3(0, surfaceHeight, size));
        }
    }

    void GenerateNodesEllipsoid(int n)
    {
        Vector3 localScale = ellipsoidTransform.localScale;
        Quaternion rotation = ellipsoidTransform.rotation;

        for (int i = 0; i < n; i++)
        {
            float alpha = Random.Range(0f, Mathf.PI);
            float theta = Random.Range(0f, Mathf.PI * 2f);
            float offset = Mathf.Pow(Random.value, 1f / 3f) / 2;

            // Generate random point inside unit sphere
            Vector3 unitPoint = new Vector3
            (
                Mathf.Cos(theta) * Mathf.Sin(alpha),
                Mathf.Sin(theta) * Mathf.Sin(alpha),
                Mathf.Cos(alpha)
            ) * offset;

            // Apply scaling
            unitPoint.x *= localScale.x;
            unitPoint.y *= localScale.y;
            unitPoint.z *= localScale.z;

            // Rotate the point
            Vector3 point = rotation * unitPoint;

            // Ensure no nodes spawn above the surfaceHeight
            if (point.y < surfaceHeight) nodes.Add(point);
        }
    }

    bool PointInsideEllipsoid(Vector3 point)
    {
        Vector3 localPoint = ellipsoidTransform.InverseTransformPoint(point);
        float xScaled = localPoint.x / ellipsoidTransform.localScale.x;
        float yScaled = localPoint.y / ellipsoidTransform.localScale.y;
        float zScaled = localPoint.z / ellipsoidTransform.localScale.z;
        return (xScaled * xScaled + yScaled * yScaled + zScaled * zScaled) <= 1.0f;
    }

    Vector3 EllipsoidBottomPoint()
    {
        Vector3 localScale = ellipsoidTransform.localScale;
        Quaternion rotation = ellipsoidTransform.rotation;

        float alpha = Mathf.PI * 0.5f;
        float theta = Mathf.PI * 1.5f;

        Vector3 unitPoint = new Vector3
        (
            Mathf.Cos(theta) * Mathf.Sin(alpha),
            Mathf.Sin(theta) * Mathf.Sin(alpha),
            Mathf.Cos(alpha)
        ) * 0.5f;

        unitPoint.x *= localScale.x;
        unitPoint.y *= localScale.y;
        unitPoint.z *= localScale.z;

        Vector3 pt = rotation * unitPoint;
        return pt;
    }
    
    private void Start()
    {
        // create nodes
        GenerateNodesEllipsoid(initialNodeAmount);

        // create entrance
        var entrance = EllipsoidBottomPoint() - Vector3.up;
        firstPassage = new Passage(entrance, entrance + new Vector3(0, passageLength, 0), new Vector3(0, 1, 0));
        passages.Add(firstPassage);
        extremities.Add(firstPassage);

        // slowly walk players
        InvokeRepeating("WalkPlayer", 0.0f, walkInterval);
        InvokeRepeating("WalkOthers", 0.0f, walkInterval * 2);
    }

    private void WalkPlayer()
    {
        if (!finished) return;

        // player
        var currentPlayer = GetParentByIndex(playerEntrance, step);
        if (currentPlayer != null)
        {
            SetPassageLight(true, currentPlayer);
            playerPassage = currentPlayer;
        }

        step++;
    }

    private void WalkOthers()
    {
        if (!finished) return;

        // others
        for (int i = 0; i < othersPassages.Count; i++)
        {
            if (othersPassages[i].parent != null)
            {
                var old = othersPassages[i];
                var next = old.parent;
                SetPassageLight(true, next);
                othersPassages[i] = next;
            }
        }
    }

    Passage GetNewHighest()
    {
        Passage highest = null;
        foreach (var ex in extremities)
        {
            bool taken = (ex == playerEntrance || othersEntrances.Contains(ex));
            if (!taken) if (highest == null || ex.end.y > highest.end.y) highest = ex;
        }
        return highest;
    }

    Passage GetParentByIndex(Passage passage, int index)
    {
        if (passage.parent == null) return null;

        Passage parent = passage;
        for (int i = 0; i < index + 1; i++) 
            if (parent.parent != null) parent = parent.parent;
            else return null;

        return parent;
    }

    private void Update()
    {
        // move camera
        if (playerEntrance != null && playerPassage != null)
        {
            var current = Camera.main.transform.position;
            var next = playerPassage.end + -Camera.main.transform.forward * cameraOffset;
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, next, cameraSpeed * Time.deltaTime);
            Camera.main.transform.RotateAround(playerPassage.end, Vector3.up, 16 * Time.deltaTime);
        }

        // iterate
        IterateSpaceColonization();

        // run once when done iterating
        if (!finished && nodes.Count == 0)
        {
            // generate mesh
            GenerateMesh();

            // assign id's
            for (int i = 0; i < passages.Count; i++) passages[i].id = i;

            // set finished to true
            finished = true;

            // set player entrance
            playerEntrance = GetNewHighest();
            SetPassageLight(true, playerEntrance);

            // set others entrances
            for (int i = 0; i < othersAmount; i++)
            {
                var otherEntrance = GetNewHighest();
                othersEntrances.Add(otherEntrance);
                SetPassageLight(true, otherEntrance);
            }

            // set others passages
            foreach (var entrance in othersEntrances) othersPassages.Add(entrance);
        }
    }

    private void IterateSpaceColonization()
    {
        // cleanup leftover nodes and return
        if (nodes.Count > 0 && nodes.Count <= nodesLeft) nodes.Clear();
        if (nodes.Count == 0) return;

        // remove nodes in killrange
        List<Vector3> toRemove = new List<Vector3>();
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < passages.Count; j++)
            {
                float distance = Vector3.Distance(nodes[i], passages[j].end);
                if (distance < killRange) toRemove.Add(nodes[i]);
            }
        }
        foreach (var node in toRemove) nodes.Remove(node);

        // reset attractors and active nodes
        for (int i = 0; i < passages.Count; i++) passages[i].attractors.Clear();
        activeNodes.Clear();

        // calculate active nodes and attractors
        for (int i = 0; i < nodes.Count; i++)
        {
            float lastDist = 10000;
            Passage closest = null;

            for (int j = 0; j < passages.Count; j++) 
            {
                float distance = Vector3.Distance(passages[j].end, nodes[i]);
                if (distance < attractionRange && distance < lastDist) 
                {
                    closest = passages[j];
                    lastDist = distance;
                }
            }

            if (closest != null)
            {
                closest.attractors.Add(nodes[i]);
                activeNodes.Add(i);
            }
        }

        // if there are nodes in attraction range
        if (activeNodes.Count != 0)
        {
            extremities.Clear();
            List<Passage> newPassages = new List<Passage>();

            for (int i = 0; i < passages.Count; i++)
            {
                if (passages[i].attractors.Count > 0)
                {
                    Vector3 dir = new Vector3(0, 0, 0);
                    for (int j = 0; j < passages[i].attractors.Count; j++) dir += (passages[i].attractors[j] - passages[i].end).normalized;
                    dir /= passages[i].attractors.Count;
                    dir += RandomGrowthVector();
                    dir.Normalize();
                    Passage passage = new Passage(passages[i].end, passages[i].end + dir * passageLength, dir, passages[i]);
                    passages[i].children.Add(passage);
                    newPassages.Add(passage);
                    extremities.Add(passage);
                } 
                else if (passages[i].children.Count == 0) extremities.Add(passages[i]);
            }

            passages.AddRange(newPassages);
        }

        // if no active nodes
        if (activeNodes.Count == 0)
        {
            for (int i = 0; i < extremities.Count; i++)
            {
                Passage current = extremities[i];
                bool extremityInRadius = PointInsideEllipsoid(current.start);
                bool beginning = passages.Count < 20;

                if (extremityInRadius || beginning)
                {
                    Vector3 raw = current.direction + RandomGrowthVector();
                    Vector3 dir = raw.normalized;
                    Passage next = new Passage(current.end, current.end + dir * passageLength, dir, current);
                    current.children.Add(next);
                    passages.Add(next);
                    extremities.Remove(current);
                    extremities.Add(next);
                }
            }
        }
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(passages.Count + 1) * subdivisions * 2];
        int[] triangles = new int[passages.Count * subdivisions * 6];

        // Construct vertices
        for (int i = 0; i < passages.Count; i++)
        {
            Passage passage = passages[i];
            int id = subdivisions * i;
            passage.verticesid = id;

            for (int j = 0; j < subdivisions; j++)
            {
                int half = (passages.Count + 1) * subdivisions;
                float part = (float)j / subdivisions * Mathf.PI * 2f;
                Quaternion ringRotation = Quaternion.FromToRotation(Vector3.up, passage.direction);
                Vector3 vertexRotation = new Vector3(Mathf.Cos(part) * passageWidth, 0, Mathf.Sin(part) * passageWidth);
                Vector3 offset = ringRotation * vertexRotation;
                Vector3 extra = passage.direction * passageWidth / 4;
                
                vertices[id + j] = passage.start - extra + offset;
                vertices[id + j + half] = passage.end + extra + offset;
            }
        }

        // Construct faces
        for (int i = 0; i < passages.Count; i++)
        {
            Passage passage = passages[i];
            int half = (passages.Count + 1) * subdivisions;

            int triangle = i * subdivisions * 6;
            int startVertex = passage.verticesid;
            int endVertex = passage.verticesid + half;

            // setup all triangles
            for (int j = 0; j < subdivisions; j++)
            {
                int offset = j == subdivisions - 1 ? 0 : j + 1;
                triangles[triangle + j * 6] = startVertex + j;
                triangles[triangle + j * 6 + 1] = endVertex + j;
                triangles[triangle + j * 6 + 2] = endVertex + offset;
                triangles[triangle + j * 6 + 3] = startVertex + j;
                triangles[triangle + j * 6 + 4] = endVertex + offset;
                triangles[triangle + j * 6 + 5] = startVertex + offset;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = InitialVertexColors(vertices.Length);
        mesh.RecalculateNormals();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.AddComponent<MeshRenderer>().material = caveMaterial;
    }

    public void SetPassageLight(bool lit, Passage passage)
    {
        Color color = lit ? Color.yellow * 2 : new Color(0.2f, 0.2f, 0.2f, 1);
        int half = (passages.Count + 1) * subdivisions;

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Color[] colors = mesh.colors;
        for (int i = 0; i < subdivisions; i++)
        {
            colors[passage.verticesid + i] = color;
            colors[passage.verticesid + i + half] = color;
        }
        mesh.colors = colors;
    }

    private Color[] InitialVertexColors(int amount)
    {
        Color[] colors = new Color[amount];
        for (int i = 0; i < passages.Count; i++)
        {
            Passage passage = passages[i];
            Color color = new Color(0.2f, 0.2f, 0.2f, 1);
            int half = (passages.Count + 1) * subdivisions;
            for (int j = 0; j < subdivisions; j++)
            {
                colors[passage.verticesid + j] = color;
                colors[passage.verticesid + j + half] = color;
            }
        }
        return colors;
    }

    private Vector3 RandomGrowthVector()
    {
        float alpha = Random.Range(0f, Mathf.PI);
        float theta = Random.Range(0f, Mathf.PI * 2f);
        Vector3 pt = new Vector3(Mathf.Cos(theta) * Mathf.Sin(alpha), Mathf.Sin(theta) * Mathf.Sin(alpha), Mathf.Cos(alpha));
        return pt * randomGrowth;
    }
}
