using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class Pipe : ChangeableMeshModelVisual3D
	{
		private Point3D m_from;
		private Point3D m_to;

		public Pipe(Point3D from, Point3D to )
		{
			m_from = from;
			m_to = to;
		}

		protected override MeshGeometry3D BuildMesh()
		{
			var builder = new MeshBuilder( false, true );
			builder.AddPipe( m_from, m_to, 0.001, 0.002, 6 );
			return builder.ToMesh();
		}
	}
}
