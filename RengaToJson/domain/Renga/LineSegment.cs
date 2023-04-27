using Renga;

namespace RengaToJson.domain.Renga;

public class LineSegment
{
	public LineSegment(FloatPoint3D p1, FloatPoint3D p2)
	{
		P1 = p1;
		P2 = p2;
	}

	public FloatPoint3D P1 { get; set; }
	public FloatPoint3D P2 { get; set; }
}