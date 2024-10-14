using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using WPF.Viewer3D.Visuals;

namespace WPF.Viewer3D
{
	public class CuttingManager
	{
		private Dictionary<Model3D, Geometry3D> m_originalGeometryData = new Dictionary<Model3D, Geometry3D>();
		private Dictionary<Model3D, Geometry3D> m_cuttedGeometryData = new Dictionary<Model3D, Geometry3D>();
		private List<CuttingData> m_cuttingData = new List<CuttingData>();

		private Visual3DCollection m_target;

		public CuttingManager( Visual3DCollection target )
		{
			m_target = target;
			//lock( this )
			//{
			//	m_target.Traverse<GeometryModel3D>( StoreGeometryData );
			//}
		}
		//private void StoreGeometryData( GeometryModel3D model, Transform3D transform )
		//{
		//	if( model == null )
		//		return;

		//	m_originalGeometryData[ model ] = model.Geometry;
		//}

		/// <summary>
		/// Добавление нового сечения.
		/// </summary>
		/// <param name="cuttingData">Данные сечения.</param>
		/// <param name="forceUpdate">Применить сечения к модели после добавления.</param>
		public void Add( CuttingData cuttingData, bool forceUpdate = true )
		{
			m_cuttingData.Add( cuttingData );

			if( forceUpdate )
			{
				Update();
			}
		}

		/// <summary>
		/// Удалить сечение.
		/// </summary>
		/// <param name="cuttingData">Данные сечения.</param>
		/// <param name="forceUpdate">Применить сечения к модели после удаления.</param>
		public void Remove( CuttingData cuttingData, bool forceUpdate = true )
		{
			m_cuttingData.Remove( cuttingData );

			if( forceUpdate )
			{
				Update();
			}
		}

		public void RemoveAll( bool forceUpdate = true )
		{
			m_cuttingData.Clear();

			if( forceUpdate )
			{
				Update();
			}
		}

		/// <summary>
		/// Применить сечения к модели.
		/// </summary>
		public void Update()
		{
			ClearCutting();

			if( m_cuttingData.Count == 0 )
			{
				m_originalGeometryData.Clear();
			}
			else
			{
				foreach( var cutting in m_cuttingData )
				{
					CreateCutting( cutting );
				}
			}
		}

		private void ClearCutting()
		{
			m_cuttedGeometryData.Clear();

			lock( this )
			{
				m_target.Traverse<GeometryModel3D>( ( m, t ) => ClearCutting( m ) );
			}
		}
		private void ClearCutting( GeometryModel3D model )
		{
			if( model == null )
				return;

			Geometry3D originalGeometry;
			if( !m_originalGeometryData.TryGetValue( model, out originalGeometry ) )
				return;

			model.Geometry = originalGeometry;
		}
		private void CreateCutting( CuttingData cuttingData )
		{
			lock( this )
			{
				m_target.Traverse<GeometryModel3D>( ( m, t ) => CreateCutting( m, t, cuttingData ) );
			}
		}
		private void CreateCutting( GeometryModel3D model, Transform3D transform, CuttingData cuttingData )
		{
			if( model == null )
				return;

			Geometry3D geometry = null;
			if( !m_cuttedGeometryData.TryGetValue( model, out geometry ) )
			{
				if( !m_originalGeometryData.TryGetValue( model, out geometry ) )
				{
					geometry = model.Geometry;
					m_originalGeometryData.Add( model, geometry );
				}
			}

			var originalMeshGeometry = geometry as MeshGeometry3D;
			if( originalMeshGeometry == null )
				return;

			var inverseTransform = transform.Inverse;
			if( inverseTransform == null )
				throw new InvalidOperationException( "No inverse transform." );

			var p = inverseTransform.Transform( cuttingData.MovedPoint );
			var p2 = inverseTransform.Transform( cuttingData.MovedPoint + cuttingData.Normal );
			var n = p2 - p;

			if( cuttingData.IsComplement )
			{
				n *= -1;
			}

			model.Geometry = originalMeshGeometry.Cut( p, n );
			m_cuttedGeometryData[ model ] = model.Geometry;
		}
	}

	public struct CuttingData : IEquatable<CuttingData>
	{
		private Vector3D m_normal;
		public Vector3D Normal
		{
			get { return m_normal; }
			set
			{
				value.Normalize();
				m_normal = value;
			}
		}

		public Point3D Point { get; }

		public Point3D MovedPoint { get; set; }

		public bool IsComplement { get; set; }

		public bool IsApplied { get; set; }

		public CuttingData( Point3D point, Vector3D normal )
		{
			Point = MovedPoint = point;

			normal.Normalize();
			m_normal = normal;

			IsComplement = true;
			IsApplied = false;
		}

		public bool Equals( CuttingData other )
		{
			return Normal.Equals( other.Normal )
				&& Point.Equals( other.Point );
		}
		public override bool Equals( object obj )
		{
			if( obj == null )
				return false;

			var nullableCuttingData = obj as CuttingData?;
			if( nullableCuttingData == null )
				return false;

			return Equals( nullableCuttingData.Value );
		}
		public override int GetHashCode()
		{
			return Normal.GetHashCode() ^ Point.GetHashCode();
		}
	}
}
