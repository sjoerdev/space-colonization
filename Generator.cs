using System.Numerics;



public class Generator
{
    // Space Colonization Settings
    public int initialNodeAmount = 1000;
    public int nodesLeft = 0;
    public float passageLength = 0.1f;
    public float attractionRange = 1;
    public float killRange = 0.6f;
    public float randomGrowth = 0.2f;

    private List<Vector3> nodes = new List<Vector3>();
    private List<int> activeNodes = new List<int>();
    private Passage firstPassage;
    private List<Passage> passages = new List<Passage>();
    private List<Passage> extremities = new List<Passage>();
    private List<Passage> othersEntrances = new List<Passage>();
    private List<Passage> othersPassages = new List<Passage>();
    
    private void Start()
    {
        // create entrance
        var entrance = Vector3.Zero;
        firstPassage = new Passage(entrance, entrance + new Vector3(0, passageLength, 0), new Vector3(0, 1, 0));
        passages.Add(firstPassage);
        extremities.Add(firstPassage);
    }

    Passage GetNewHighest()
    {
        Passage highest = null;
        foreach (var ex in extremities)
        {
            bool taken = ex == playerEntrance || othersEntrances.Contains(ex);
            if (!taken) if (highest == null || ex.end.Y > highest.end.Y) highest = ex;
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
                    dir = Vector3.Normalize(dir);
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
                bool extremityInRadius = IsPointInsideGrowthRegion(current.start);
                bool beginning = passages.Count < 20;

                if (extremityInRadius || beginning)
                {
                    Vector3 raw = current.direction + RandomGrowthVector();
                    Vector3 dir = Vector3.Normalize(raw);
                    Passage next = new Passage(current.end, current.end + dir * passageLength, dir, current);
                    current.children.Add(next);
                    passages.Add(next);
                    extremities.Remove(current);
                    extremities.Add(next);
                }
            }
        }
    }

    private bool IsPointInsideGrowthRegion(Vector3 point)
    {
        return true; // todo
    }

    private Vector3 RandomGrowthVector()
    {
        float alpha = Random.Range(0f, MathF.PI);
        float theta = Random.Range(0f, MathF.PI * 2f);
        Vector3 pt = new Vector3(MathF.Cos(theta) * MathF.Sin(alpha), MathF.Sin(theta) * MathF.Sin(alpha), MathF.Cos(alpha));
        return pt * randomGrowth;
    }
}
