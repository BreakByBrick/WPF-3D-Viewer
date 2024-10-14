using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal static class Visual3DEx
	{
		public static Matrix3D GetTransform( this Visual3D visual )
		{
			var totalTransform = Matrix3D.Identity;

			DependencyObject obj = visual;
			while( obj != null )
			{
				if( obj is Viewport3DVisual viewport3DVisual )
				{
					return totalTransform;
				}
				else if( obj is Visual3D mv && mv.Transform != null )
				{
					totalTransform.Append( mv.Transform.Value );
				}
				obj = VisualTreeHelper.GetParent( obj );
			}

			throw new InvalidOperationException( "The visual is not added to a Viewport3D." );
		}
		public static GeneralTransform3D GetTransformTo( this Visual3D visual, Model3D model )
		{
			var mc = GetModel( visual );
			if( mc != null )
			{
				return mc.GetTransform( model, Transform3D.Identity );
			}

			return null;
		}

		public static Viewport3D GetViewport3D( this Visual3D visual )
		{
			DependencyObject obj = visual;
			while( obj != null )
			{
				var vis = obj as Viewport3DVisual;
				if( vis != null )
				{
					return VisualTreeHelper.GetParent( obj ) as Viewport3D;
				}

				obj = VisualTreeHelper.GetParent( obj );
			}

			return null;
		}

		public static double GetModelRadius( this Visual3D visual )
		{
			var modelBounds = visual.GetModelBounds( Transform3D.Identity );
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelDiagonal.Length * 0.5;
		}

		public static Rect3D GetModelBounds( this Visual3D visual, Transform3D transform )
		{
			var bounds = Rect3D.Empty;
			var childTransform = TransformHelper.CombineTransform( visual.Transform, transform );
			var model = GetModel( visual );
			if( model != null )
			{
				var transformedBounds = childTransform.TransformBounds( model.Bounds );
				if( !double.IsNaN( transformedBounds.X ) )
				{
					bounds.Union( transformedBounds );
				}
			}

			foreach( var child in GetChildren( visual ) )
			{
				var b = GetModelBounds( child, childTransform );
				bounds.Union( b );
			}

			return bounds;
		}


		private static readonly PropertyInfo Visual3DModelPropertyInfo = typeof( Visual3D ).GetProperty( "Visual3DModel", BindingFlags.Instance | BindingFlags.NonPublic );

		private static Model3D GetModel( this Visual3D visual )
		{
			Model3D model;
			var mv = visual as ModelVisual3D;
			if( mv != null )
			{
				model = mv.Content;
			}
			else
			{
				model = Visual3DModelPropertyInfo.GetValue( visual, null ) as Model3D;
			}

			return model;
		}

		public static IEnumerable<Visual3D> GetChildren( this Visual3D parent )
		{
			int n = VisualTreeHelper.GetChildrenCount( parent );
			for( int i = 0; i < n; i++ )
			{
				var child = VisualTreeHelper.GetChild( parent, i ) as Visual3D;
				if( child == null )
				{
					continue;
				}

				yield return child;
			}
		}

		/// <summary>
		/// Обход дерева Visual3D/Model3D и вызов указанного действия для каждого Model3D заданного типа.
		/// </summary>
		public static void Traverse<T>( this Visual3D visual, Action<T, Transform3D> action ) where T : Model3D
		{
			Traverse( visual, Transform3D.Identity, action );
		}
	
		private static void Traverse<T>( Visual3D visual, Transform3D transform, Action<T, Visual3D, Transform3D> action ) where T : Model3D
		{
			var childTransform = TransformHelper.CombineTransform( visual.Transform, transform );
			var model = visual.GetModel();
			if( model != null )
			{
				model.Traverse( visual, childTransform, action );
			}

			foreach( var child in visual.GetChildren() )
			{
				Traverse( child, childTransform, action );
			}
		}

		private static void Traverse<T>( Visual3D visual, Transform3D transform, Action<T, Transform3D> action )
			where T : Model3D
		{
			var childTransform = TransformHelper.CombineTransform( visual.Transform, transform );
			var model = visual.GetModel();
			if( model != null )
			{
				model.Traverse( childTransform, action );
			}

			foreach( var child in visual.GetChildren() )
			{
				Traverse( child, childTransform, action );
			}
		}

		public static void TraverseVisuals<T>( this Visual3D visual, Action<T, Transform3D> action ) where T : Visual3D
		{
			TraverseVisuals( visual, Transform3D.Identity, action );
		}

		private static void TraverseVisuals<T>( Visual3D visual, Transform3D transform, Action<T, Transform3D> action )
				where T : Visual3D
		{
			var childTransform = TransformHelper.CombineTransform( visual.Transform, transform );

			var children = visual.GetChildren();
			if( children.Any() )
			{
				foreach( var child in children )
				{
					TraverseVisuals( child, childTransform, action );
				}
			}
			else
			{
				action( ( T )visual, childTransform );
			}
		}
	}
}
