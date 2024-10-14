using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public static class Model3DGroupEx
	{
		public static Point3D GetModelCenter( this Model3DGroup modelGroup )
		{
			var modelBounds = modelGroup.GetModelBounds();
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelBounds.Location + ( modelDiagonal * 0.5 );
		}
		public static Point3D GetModelCenter( this Model3DGroup modelGroup, Rect3D modelBounds )
		{
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelBounds.Location + ( modelDiagonal * 0.5 );
		}
		public static Rect3D GetModelBounds( this Model3DGroup modelGroup )
		{
			return modelGroup.Children.GetModelBounds();
		}

		public static double GetModelRadius( this Model3DGroup modelGroup )
		{
			var modelBounds = modelGroup.GetModelBounds();
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelDiagonal.Length * 0.5;
		}
		public static double GetModelRadius( this Model3DGroup modelGroup, Rect3D modelBounds )
		{
			var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
			return modelDiagonal.Length * 0.5;
		}
	}
}
