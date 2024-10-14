using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Point3DEx
	{
		public static Vector3D ToVector3D( this Point3D n )
		{
			return new Vector3D( n.X, n.Y, n.Z );
		}
		public static Point3D Multiply( this Point3D p, double d )
		{
			return new Point3D( p.X * d, p.Y * d, p.Z * d );
		}
	}
}
