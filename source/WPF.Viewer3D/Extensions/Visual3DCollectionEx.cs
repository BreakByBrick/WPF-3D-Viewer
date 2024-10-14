using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Visual3DCollectionEx
	{
		public static Rect3D GetModelBounds( this Visual3DCollection children )
		{
			var bounds = Rect3D.Empty;
			foreach( var visual in children )
			{
				var b = visual.GetModelBounds( Transform3D.Identity );
				bounds.Union( b );
			}
			return bounds;
		}

		/// <summary>
		/// Обход дерева Visual3D/Model3D и вызов указанного действия для каждого Model3D заданного типа.
		/// </summary>
		public static void Traverse<T>( this Visual3DCollection visuals, Action<T, Transform3D> action ) where T : Model3D
		{
			foreach( var child in visuals )
			{
				child.Traverse( action );
			}
		}

		public static void TraverseVisuals<T>( this Visual3DCollection visuals, Action<T, Transform3D> action ) where T : Visual3D
		{
			foreach( var child in visuals )
			{
				child.TraverseVisuals( action );
			}
		}
	}
}
