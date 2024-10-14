using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal class TransformHelper
	{
		public static Transform3D CombineTransform( Transform3D t1, Transform3D t2 )
		{
			if( t1 == null && t2 == null )
				return Transform3D.Identity;
			if( t1 == null && t2 != null )
				return t2;
			if( t1 != null && t2 == null )
				return t1;
			var g = new MatrixTransform3D( t1.Value * t2.Value );
			return g;
		}
	}
}
