using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class ProjectionCameraEx
	{
		public static Matrix3D GetViewMatrix( this ProjectionCamera camera )
		{
			if( camera == null )
				throw new ArgumentNullException( nameof( camera ) );

			var zaxis = -camera.LookDirection;
			zaxis.Normalize();

			var xaxis = Vector3D.CrossProduct( camera.UpDirection, zaxis );
			xaxis.Normalize();

			var yaxis = Vector3D.CrossProduct( zaxis, xaxis );
			var pos = ( Vector3D )camera.Position;

			return new Matrix3D(
				xaxis.X, yaxis.X, zaxis.X, 0,
				xaxis.Y, yaxis.Y, zaxis.Y, 0,
				xaxis.Z, yaxis.Z, zaxis.Z, 0,
				-Vector3D.DotProduct( xaxis, pos ),
				-Vector3D.DotProduct( yaxis, pos ),
				-Vector3D.DotProduct( zaxis, pos ),
				1 );

		}
	}
}
