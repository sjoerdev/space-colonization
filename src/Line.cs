using System.Numerics;

public class Line
{
    public Vector3 start;
    public Vector3 end;
    public Vector3 direction;
    public Line parent;
    public List<Line> children = new List<Line>();
    public List<Vector3> attractors = new List<Vector3>();
    
    public Line(Vector3 start, Vector3 end, Vector3 direction, Line parent = null)
    {
        this.start = start;
        this.end = end;
        this.direction = direction;
        this.parent = parent;
    }
}