using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Vector3DEx
	{
		/// <summary>
		/// Получение перпендикулярного вектора.
		/// </summary>
		public static Vector3D GetPerpendicular( this Vector3D n, bool normalized = true )
		{
			n.Normalize();

			Vector3D u = Vector3D.CrossProduct( new Vector3D( 0, 1, 0 ), n );
			if( u.LengthSquared < 1e-3 )
			{
				u = Vector3D.CrossProduct( new Vector3D( 1, 0, 0 ), n );
			}

			if( normalized )
			{
				u.Normalize();
			}

			return u;
		}
	}
}
