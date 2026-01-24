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
    public List<int> allAttractors = new List<int>();
    public List<Line> lines = new List<Line>();
    public List<Line> extremities = new List<Line>();

    public void Initialize()
    {
        // generate nodes
        GenerateNodes(initialNodeAmount, 5);

        // create first line
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

        // remove nodes in killrange (nodes that are too close to a line)
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

        // reset attractors
        for (int i = 0; i < lines.Count; i++) lines[i].attractors.Clear();
        allAttractors.Clear();

        // calculate which nodes are in attraction range of a line
        for (int i = 0; i < nodes.Count; i++)
        {
            float lastDist = 10000;
            Line closestLine = null;

            for (int j = 0; j < lines.Count; j++)
            {
                float distance = Vector3.Distance(lines[j].end, nodes[i]);
                if (distance < attractionRange && distance < lastDist)
                {
                    closestLine = lines[j];
                    lastDist = distance;
                }
            }

            if (closestLine != null)
            {
                closestLine.attractors.Add(nodes[i]);
                allAttractors.Add(i);
            }
        }

        // if there are nodes in attraction range of any line
        if (allAttractors.Count != 0)
        {
            extremities.Clear();
            List<Line> newLines = new List<Line>();

            for (int i = 0; i < lines.Count; i++) // for each line
            {
                if (lines[i].attractors.Count > 0) // for each attractor of that line
                {
                    // get direction in average direction of all attractors
                    Vector3 dir = new Vector3(0, 0, 0);
                    for (int j = 0; j < lines[i].attractors.Count; j++)
                    {
                        var diff = lines[i].attractors[j] - lines[i].end;
                        dir += Vector3.Normalize(diff);
                    }
                    dir /= lines[i].attractors.Count;

                    // add random offset to direction
                    dir += RandomGrowthVector();
                    dir = Vector3.Normalize(dir);

                    // create new line that goes in that direction
                    Line line = new Line(lines[i].end, lines[i].end + dir * lineLength, dir, lines[i]);
                    lines[i].children.Add(line);

                    // make this new line the new extremity
                    newLines.Add(line);
                    extremities.Add(line);
                }
                else // if the line has no attractors
                {
                    if (lines[i].children.Count == 0) // if the line has no children
                    {
                        extremities.Add(lines[i]);
                    }
                }
            }

            lines.AddRange(newLines);
        }

        // if no nodes attract any line then make lines grow randomly
        if (allAttractors.Count == 0)
        {
            for (int i = 0; i < extremities.Count; i++)
            {
                Line current = extremities[i];
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

    private Vector3 RandomGrowthVector()
    {
        float alpha = Random.Shared.NextSingle() * MathF.PI;
        float theta = Random.Shared.NextSingle() * MathF.PI * 2f;
        Vector3 offset = new Vector3(MathF.Cos(theta) * MathF.Sin(alpha), MathF.Sin(theta) * MathF.Sin(alpha), MathF.Cos(alpha));
        return offset * randomGrowth;
    }
}