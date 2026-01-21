using System.Numerics;

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