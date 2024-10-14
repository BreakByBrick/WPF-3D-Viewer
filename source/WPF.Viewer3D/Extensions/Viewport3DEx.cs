using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal static class Viewport3DEx
	{
		public static double GetModelRadius( this Viewport3D viewport )
		{
			var modelBounds = viewport.GetModelBounds();
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelDiagonal.Length * 0.5;
		}
		public static double GetModelRadius( this Viewport3D viewport, Rect3D modelBounds )
		{
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelDiagonal.Length * 0.5;
		}
		public static Point3D GetModelCenter( this Viewport3D viewport )
		{
			var modelBounds = viewport.GetModelBounds();
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelBounds.Location + ( modelDiagonal * 0.5 );
		}
		public static Point3D GetModelCenter( this Viewport3D viewport, Rect3D modelBounds )
		{
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelBounds.Location + ( modelDiagonal * 0.5 );
		}
		public static Rect3D GetModelBounds( this Viewport3D viewport )
		{
			return viewport.Children.GetModelBounds();
		}
		public static Matrix3D GetViewportTransform( this Viewport3D viewport )
		{
			return new Matrix3D(
				viewport.ActualWidth / 2,
				0,
				0,
				0,
				0,
				-viewport.ActualHeight / 2,
				0,
				0,
				0,
				0,
				1,
				0,
				viewport.ActualWidth / 2,
				viewport.ActualHeight / 2,
				0,
				1 );
		}
		public static Matrix3D GetCameraTransform( this Viewport3D viewport )
		{
			return viewport.Camera.GetTotalTransform( viewport.ActualWidth / viewport.ActualHeight );
		}
		public static GeneralTransform3D GetTransform( this Viewport3D viewport, Visual3D visual )
		{
			if( visual == null )
			{
				return null;
			}

			foreach( var ancestor in viewport.Children )
			{
				if( visual.IsDescendantOf( ancestor ) )
				{
					var g = new GeneralTransform3DGroup();

					// this includes the visual.Transform
					var ta = visual.TransformToAncestor( ancestor );
					if( ta != null )
					{
						g.Children.Add( ta );
					}

					// add the transform of the top-level ancestor
					g.Children.Add( ancestor.Transform );

					return g;
				}
			}

			return visual.Transform;
		}
		public static bool TryHitModel( this Viewport3D viewport, Point cursotPosition, out GeometryModel3D hitModel )
		{
			var camera = viewport.Camera as ProjectionCamera;
			if( camera == null )
			{
				hitModel = new GeometryModel3D();
				return false;
			}

			var isHit = false;
			var model = new GeometryModel3D();

			VisualTreeHelper.HitTest( viewport, null, ( r ) => ModelHitTestCallback( r, viewport, camera.Position, out isHit, out model ), new PointHitTestParameters( cursotPosition ) );

			hitModel = model;
			return isHit;
		}

		private static HitTestResultBehavior ModelHitTestCallback( HitTestResult result, Viewport3D viewport, Point3D cameraPosition, out bool isHit, out GeometryModel3D hitModel )
		{
			var rayHit = result as RayMeshGeometry3DHitTestResult;
			if( rayHit != null )
			{
				var geometryModel = rayHit.ModelHit as GeometryModel3D;
				if( geometryModel != null )
				{
					isHit = true;
					hitModel = geometryModel;
					return HitTestResultBehavior.Stop;
				}
			}

			isHit = false;
			hitModel = new GeometryModel3D();
			return HitTestResultBehavior.Continue;
		}

		public static bool TryHitPoint( this Viewport3D viewport, Point cursotPosition, out Point3D hitPoint )
		{
			var camera = viewport.Camera as ProjectionCamera;
			if( camera == null )
			{
				hitPoint = new Point3D();
				return false;
			}

			var isHit = false;
			var point = new Point3D();

			VisualTreeHelper.HitTest( viewport, null, ( r ) => PointHitTestCallback( r, viewport, camera.Position, out isHit, out point ), new PointHitTestParameters( cursotPosition ) );

			hitPoint = point;
			return isHit;
		}

		private static HitTestResultBehavior PointHitTestCallback( HitTestResult result, Viewport3D viewport, Point3D cameraPosition, out bool isHit, out Point3D hitPoint )
		{
			var rayHit = result as RayMeshGeometry3DHitTestResult;
			if( rayHit == null )
			{
				isHit = false;
				hitPoint = new Point3D();
				return HitTestResultBehavior.Continue;
			}

			var mesh = rayHit.MeshHit;
			if( mesh == null )
			{
				isHit = false;
				hitPoint = new Point3D();
				return HitTestResultBehavior.Continue;
			}

			var p1 = mesh.Positions[ rayHit.VertexIndex1 ];
			var p2 = mesh.Positions[ rayHit.VertexIndex2 ];
			var p3 = mesh.Positions[ rayHit.VertexIndex3 ];

			double x = ( p1.X * rayHit.VertexWeight1 ) + ( p2.X * rayHit.VertexWeight2 ) + ( p3.X * rayHit.VertexWeight3 );
			double y = ( p1.Y * rayHit.VertexWeight1 ) + ( p2.Y * rayHit.VertexWeight2 ) + ( p3.Y * rayHit.VertexWeight3 );
			double z = ( p1.Z * rayHit.VertexWeight1 ) + ( p2.Z * rayHit.VertexWeight2 ) + ( p3.Z * rayHit.VertexWeight3 );

			var p = new Point3D( x, y, z );

			// transform to global coordinates
			// first transform the Model3D hierarchy
			var t2 = rayHit.VisualHit.GetTransformTo( rayHit.ModelHit );
			if( t2 != null )
			{
				p = t2.Transform( p );
			}

			// then transform the Visual3D hierarchy up to the Viewport3D ancestor
			var t = viewport.GetTransform( rayHit.VisualHit );
			if( t != null )
			{
				p = t.Transform( p );
			}

			double distance = ( cameraPosition - p ).LengthSquared;
			if( distance < double.MaxValue )
			{
				isHit = true;
				hitPoint = p;
				return HitTestResultBehavior.Stop;
			}

			isHit = false;
			hitPoint = new Point3D();
			return HitTestResultBehavior.Continue;
		}

		public static Point Point3DtoPoint2D( this Viewport3D viewport, Point3D point )
		{
			var matrix = GetTotalTransform( viewport );
			var pointTransformed = matrix.Transform( point );
			var pt = new Point( pointTransformed.X, pointTransformed.Y );
			return pt;
		}
		public static Matrix3D GetTotalTransform( this Viewport3D viewport )
		{
			var transform = GetCameraTransform( viewport );
			transform.Append( GetViewportTransform( viewport ) );
			return transform;
		}
	}
}
