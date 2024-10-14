using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Model3DEx
	{
		public static GeneralTransform3D GetTransform( this Model3D current, Model3D model, Transform3D parentTransform )
		{
			var currentTransform = TransformHelper.CombineTransform( current.Transform, parentTransform );
			if( ReferenceEquals( current, model ) )
			{
				return currentTransform;
			}

			var mg = current as Model3DGroup;
			if( mg != null )
			{
				foreach( var m in mg.Children )
				{
					var result = GetTransform( m, model, currentTransform );
					if( result != null )
					{
						return result;
					}
				}
			}

			return null;
		}

		public static Rect3D GetModelBounds( this Model3D model, Transform3D transform )
		{
			var bounds = Rect3D.Empty;
			if( !double.IsNaN( model.Bounds.X ) )
			{
				bounds.Union( model.Bounds );
			}
			return bounds;
		}

		/// <summary>
		/// Обход дерева Model3D и вызов указанного действия для каждого Model3D заданного типа.
		/// </summary>
		public static void Traverse<T>( this Model3D model, Transform3D transform, Action<T, Transform3D> action )
			where T : Model3D
		{
			var mg = model as Model3DGroup;
			if( mg != null )
			{
				var childTransform = TransformHelper.CombineTransform( model.Transform, transform );
				foreach( var m in mg.Children )
				{
					Traverse( m, childTransform, action );
				}
			}

			var gm = model as T;
			if( gm != null )
			{
				var childTransform = TransformHelper.CombineTransform( model.Transform, transform );
				action( gm, childTransform );
			}
		}

		/// <summary>
		/// Обход дерева Model3D и вызов указанного действия для каждого Model3D заданного типа.
		/// </summary>
		public static void Traverse<T>( this Model3D model, Visual3D visual, Transform3D transform, Action<T, Visual3D, Transform3D> action )
			where T : Model3D
		{
			var mg = model as Model3DGroup;
			if( mg != null )
			{
				var childTransform = TransformHelper.CombineTransform( model.Transform, transform );
				foreach( var m in mg.Children )
				{
					Traverse( m, visual, childTransform, action );
				}
			}

			var gm = model as T;
			if( gm != null )
			{
				var childTransform = TransformHelper.CombineTransform( model.Transform, transform );
				action( gm, visual, childTransform );
			}
		}
	}
}
