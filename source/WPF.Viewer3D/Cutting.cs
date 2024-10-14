using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal class Cutting
	{
		private Visual3DCollection m_cutObjects;
		private CuttingPlane m_cuttingPlane;

		private bool m_isCutted = false;


		private bool m_complement;


		private Dictionary<Model3D, Geometry3D> m_originalGeometryData;
		public Dictionary<Model3D, Geometry3D> OriginalGeometryData
		{
			get
			{
				return m_originalGeometryData;
			}
			set
			{
				m_originalGeometryData = value;
			}
		}



		public Cutting( Visual3DCollection cutObjects, CuttingPlane cuttingPlane, bool complement )
		{
			m_cutObjects = cutObjects;
			m_cuttingPlane = cuttingPlane;
			m_complement = complement;

			m_originalGeometryData = new Dictionary<Model3D, Geometry3D>();
		}

		public void Toggle()
		{
			lock( this )
			{
				m_cutObjects.Traverse<GeometryModel3D>( ( m, t ) =>
				{
					if( m_isCutted ) Reset( m );
					else Apply( m, t );
				} );

				m_isCutted = !m_isCutted;
			}
		}

		public void Apply()
		{
			if( m_isCutted )
				return;

			lock( this )
			{
				m_cutObjects.Traverse<GeometryModel3D>( ( m, t ) => Apply( m, t ) );
				m_isCutted = true;
			}
		}
		private void Apply( GeometryModel3D model, Transform3D transform )
		{
			if( model == null )
				return;

			Geometry3D originalGeometry;
			if( !m_originalGeometryData.TryGetValue( model, out originalGeometry ) )
			{
				originalGeometry = model.Geometry;
				m_originalGeometryData.Add( model, originalGeometry );
			}

			var originalMeshGeometry = originalGeometry as MeshGeometry3D;
			if( originalMeshGeometry == null )
				return;

			var inverseTransform = transform.Inverse;
			if( inverseTransform == null )
				throw new InvalidOperationException( "No inverse transform." );

			model.Geometry = this.Intersect( originalMeshGeometry, inverseTransform, m_cuttingPlane, m_complement );
		}

		public void Reset()
		{
			if( !m_isCutted )
				return;

			lock( this )
			{
				m_cutObjects.Traverse<GeometryModel3D>( ( m, t ) => Reset( m ) );
				m_isCutted = false;
			}
		}
		private void Reset( GeometryModel3D model )
		{
			if( model == null )
				return;

			Geometry3D originalGeometry;
			if( !m_originalGeometryData.TryGetValue( model, out originalGeometry ) )
				return;

			model.Geometry = originalGeometry;
		}

		private MeshGeometry3D Intersect( MeshGeometry3D source, GeneralTransform3D inverseTransform, CuttingPlane plane, bool complement )
		{
			var p = inverseTransform.Transform( plane.Position );
			var p2 = inverseTransform.Transform( plane.Position + plane.Normal );
			var n = p2 - p;

			if( complement )
			{
				n *= -1;
			}

			return source.Cut( p, n );
		}
	}
}
