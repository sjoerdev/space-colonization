using System.Numerics;

public class Simulation
{
    public int initialNodeAmount = 1000;
    public int nodesLeft = 0;
    public float lineLength = 0.1f;
    public float attractionRange = 1;
    public float killRange = 0.6f;
    public float randomGrowth = 0.2f;

    public List<Vector3> nodes = new List<Vector3>();
    public List<int> activeNodes = new List<int>();
    public List<Line> lines = new List<Line>();
    public List<Line> extremities = new List<Line>();
    
    public void Initialize()
    {
        // generate nodes
        GenerateNodes(initialNodeAmount, 5);

        // create entrance
        var entrance = Vector3.Zero;
        Line firstLine = new Line(entrance, entrance + new Vector3(0, lineLength, 0), new Vector3(0, 1, 0));
        lines.Add(firstLine);
        extremities.Add(firstLine);
    }

    public void IterateSpaceColonization()
    {
        // cleanup leftover nodes and return
        if (nodes.Count > 0 && nodes.Count <= nodesLeft) nodes.Clear();
        if (nodes.Count == 0) return;

        // remove nodes in killrange
        List<Vector3> toRemove = new List<Vector3>();
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < lines.Count; j++)
            {
                float distance = Vector3.Distance(nodes[i], lines[j].end);
                if (distance < killRange) toRemove.Add(nodes[i]);
            }
        }
        foreach (var node in toRemove) nodes.Remove(node);

        // reset attractors and active nodes
        for (int i = 0; i < lines.Count; i++) lines[i].attractors.Clear();
        activeNodes.Clear();

        // calculate active nodes and attractors
        for (int i = 0; i < nodes.Count; i++)
        {
            float lastDist = 10000;
            Line closest = null;

            for (int j = 0; j < lines.Count; j++) 
            {
                float distance = Vector3.Distance(lines[j].end, nodes[i]);
                if (distance < attractionRange && distance < lastDist) 
                {
                    closest = lines[j];
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
            List<Line> newLines = new List<Line>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].attractors.Count > 0)
                {
                    Vector3 dir = new Vector3(0, 0, 0);
                    for (int j = 0; j < lines[i].attractors.Count; j++)
                    {
                        var diff = lines[i].attractors[j] - lines[i].end;
                        dir += Vector3.Normalize(diff);
                    }
                    dir /= lines[i].attractors.Count;
                    dir += RandomGrowthVector();
                    dir = Vector3.Normalize(dir);
                    Line line = new Line(lines[i].end, lines[i].end + dir * lineLength, dir, lines[i]);
                    lines[i].children.Add(line);
                    newLines.Add(line);
                    extremities.Add(line);
                } 
                else if (lines[i].children.Count == 0) extremities.Add(lines[i]);
            }

            lines.AddRange(newLines);
        }

        // if no active nodes
        if (activeNodes.Count == 0)
        {
            for (int i = 0; i < extremities.Count; i++)
            {
                Line current = extremities[i];
                bool extremityInRadius = IsPointInsideGrowthRegion(current.start);
                bool beginning = lines.Count < 20;

                if (extremityInRadius || beginning)
                {
                    Vector3 raw = current.direction + RandomGrowthVector();
                    Vector3 dir = Vector3.Normalize(raw);
                    Line next = new Line(current.end, current.end + dir * lineLength, dir, current);
                    current.children.Add(next);
                    lines.Add(next);
                    extremities.Remove(current);
                    extremities.Add(next);
                }
            }
        }
    }

    void GenerateNodes(int number, int radius)
    {
        for (int i = 0; i < number; i++)
        {
            float alpha = Random.Shared.NextSingle() * MathF.PI;
            float theta = Random.Shared.NextSingle() * MathF.PI * 2f;
            float offset = MathF.Pow(Random.Shared.NextSingle(), 1f / 3f) / 2;

            var p = new Vector3(
                MathF.Cos(theta) * MathF.Sin(alpha),
                MathF.Sin(theta) * MathF.Sin(alpha),
                MathF.Cos(alpha)
            ) * offset * radius;

            nodes.Add(p);
        }
    }

    private bool IsPointInsideGrowthRegion(Vector3 point)
    {
        return true; // todo
    }

    private Vector3 RandomGrowthVector()
    {
        float alpha = Random.Shared.NextSingle() * MathF.PI;
        float theta = Random.Shared.NextSingle() * MathF.PI * 2f;
        Vector3 offset = new Vector3(MathF.Cos(theta) * MathF.Sin(alpha), MathF.Sin(theta) * MathF.Sin(alpha), MathF.Cos(alpha));
        return offset * randomGrowth;
    }
}