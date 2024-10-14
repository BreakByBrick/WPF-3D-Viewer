using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal class CuttingPlane
	{
		private Vector3D m_normal;
		public Vector3D Normal
		{
			get
			{
				return this.m_normal;
			}

			set
			{
				this.m_normal = value;
			}
		}

		private Point3D m_position;
		public Point3D Position
		{
			get
			{
				return this.m_position;
			}

			set
			{
				this.m_position = value;
			}
		}

		public CuttingPlane( Point3D position, Vector3D normal )
		{
			m_position = position;
			m_normal = normal;
		}
	}
}
