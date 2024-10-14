using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal static class CameraEx
	{
		public static Matrix3D GetViewMatrix( this Camera camera )
		{
			if( camera == null )
			{
				throw new ArgumentNullException( nameof( camera ) );
			}

			var matrixCamera = camera as MatrixCamera;
			if( matrixCamera != null )
			{
				return matrixCamera.ViewMatrix;
			}

			var projectionCamera = camera as ProjectionCamera;
			if( projectionCamera != null )
			{
				var zaxis = -projectionCamera.LookDirection;
				zaxis.Normalize();

				var xaxis = Vector3D.CrossProduct( projectionCamera.UpDirection, zaxis );
				xaxis.Normalize();

				var yaxis = Vector3D.CrossProduct( zaxis, xaxis );
				var pos = ( Vector3D )projectionCamera.Position;

				return new Matrix3D(
					xaxis.X,
					yaxis.X,
					zaxis.X,
					0,
					xaxis.Y,
					yaxis.Y,
					zaxis.Y,
					0,
					xaxis.Z,
					yaxis.Z,
					zaxis.Z,
					0,
					-Vector3D.DotProduct( xaxis, pos ),
					-Vector3D.DotProduct( yaxis, pos ),
					-Vector3D.DotProduct( zaxis, pos ),
					1 );
			}

			throw new Exception( "Unknown camera type." );
		}

		public static Matrix3D GetTotalTransform( this Camera camera, double aspectRatio )
		{
			var m = Matrix3D.Identity;

			if( camera == null )
			{
				throw new ArgumentNullException( nameof( camera ) );
			}

			if( camera.Transform != null )
			{
				var cameraTransform = camera.Transform.Value;

				if( !cameraTransform.HasInverse )
				{
					throw new Exception( "Camera transform has no inverse." );
				}

				cameraTransform.Invert();
				m.Append( cameraTransform );
			}

			m.Append( GetViewMatrix( camera ) );
			m.Append( GetProjectionMatrix( camera, aspectRatio ) );
			return m;
		}

		public static Matrix3D GetProjectionMatrix( this Camera camera, double aspectRatio )
		{
			if( camera == null )
			{
				throw new ArgumentNullException( nameof( camera ) );
			}

			var perspectiveCamera = camera as PerspectiveCamera;
			if( perspectiveCamera != null )
			{
				// The angle-to-radian formula is a little off because only
				// half the angle enters the calculation.
				double xscale = 1 / Math.Tan( Math.PI * perspectiveCamera.FieldOfView / 360 );
				double yscale = xscale * aspectRatio;
				double znear = perspectiveCamera.NearPlaneDistance;
				double zfar = perspectiveCamera.FarPlaneDistance;
				double zscale = double.IsPositiveInfinity( zfar ) ? -1 : ( zfar / ( znear - zfar ) );
				double zoffset = znear * zscale;

				return new Matrix3D( xscale, 0, 0, 0, 0, yscale, 0, 0, 0, 0, zscale, -1, 0, 0, zoffset, 0 );
			}

			var orthographicCamera = camera as OrthographicCamera;
			if( orthographicCamera != null )
			{
				double xscale = 2.0 / orthographicCamera.Width;
				double yscale = xscale * aspectRatio;
				double znear = orthographicCamera.NearPlaneDistance;
				double zfar = orthographicCamera.FarPlaneDistance;

				if( double.IsPositiveInfinity( zfar ) )
				{
					zfar = znear * 1e5;
				}

				double dzinv = 1.0 / ( znear - zfar );

				var m = new Matrix3D( xscale, 0, 0, 0, 0, yscale, 0, 0, 0, 0, dzinv, 0, 0, 0, znear * dzinv, 1 );
				return m;
			}

			var matrixCamera = camera as MatrixCamera;
			if( matrixCamera != null )
			{
				return matrixCamera.ProjectionMatrix;
			}

			throw new Exception( "Unknown camera type." );
		}
	}
}
