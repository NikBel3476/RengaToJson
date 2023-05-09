using System;
using NUnit.Framework;
using Renga;

namespace RengaToJson.Tests
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void Test1()
		{
			var point1 = new FloatPoint3D { X = 0, Y = 0, Z = 0 };
			var point2 = new FloatPoint3D { X = 1, Y = 1, Z = 0 };

			var distance = Convert.ToSingle(Math.Sqrt(2));
			Assert.AreEqual(distance, RengaToJsonPlugin.Distance(point1, point2));
		}
	}
}