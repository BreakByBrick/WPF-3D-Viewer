using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Model3DCollectionEx
	{
		public static Rect3D GetModelBounds( this Model3DCollection children )
		{
			var bounds = Rect3D.Empty;
			foreach( var model in children )
			{
				var b = model.GetModelBounds( Transform3D.Identity );
				bounds.Union( b );
			}
			return bounds;
		}
	}
}
