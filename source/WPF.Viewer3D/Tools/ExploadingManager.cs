using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WPF.Viewer3D.Visuals;

namespace WPF.Viewer3D
{
	public class ExplodingManager
	{
		private Dictionary<Visual3D, Transform3D> m_originalTransforms = new Dictionary<Visual3D, Transform3D>();
		private Dictionary<Visual3D, ExplodeData> m_explodeData = new Dictionary<Visual3D, ExplodeData>();
		private double m_maxModelDiagonal;

		private Visual3DCollection m_target;
		private Point3D m_explosionPoint;

		public ExplodingManager( Visual3DCollection target, Point3D explosionPoint )
		{
			m_target = target;
			m_explosionPoint = explosionPoint;
			//lock( this )
			//{
			//	m_target.TraverseVisuals<Visual3D>( ( v, t ) => CalculateExplosionVector( v, t, explosionPoint ) );
			//}
		}
		//private void CalculateExplosionVector( Visual3D visual, Transform3D transform, Point3D explosionPoint )
		//{
		//	m_originalTransforms[ visual ] = transform;

		//	var modelBounds = visual.GetModelBounds( transform );
		//	if( modelBounds != null )
		//	{
		//		var modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
		//		var modelCenter = modelBounds.Location + ( modelDiagonal * 0.5 );
		//		var explosionVector = modelCenter - explosionPoint;

		//		m_explosionVectors[ visual ] = explosionVector;
		//	}
		//}

		public void Apply( double delta )
		{
			lock( this )
			{
				m_target.TraverseVisuals<Visual3D>( ( m, t ) => Apply( m, delta ) );
			}

			if( delta == 0 )
			{
				m_explodeData.Clear();
			}
		}
		private void Apply( Visual3D model, double delta )
		{
			if( model == null )
				return;

			Transform3D transform;
			if( !m_originalTransforms.TryGetValue( model, out transform ) )
			{
				m_originalTransforms[ model ] = transform = model.Transform;
			}

			ExplodeData explodeData;
			if( !m_explodeData.TryGetValue( model, out explodeData ) )
			{
				var modelBounds = model.GetModelBounds( Transform3D.Identity );
				if( ( modelBounds != null ) && !modelBounds.IsEmpty )
				{
					Vector3D modelDiagonal = new Vector3D( modelBounds.SizeX, modelBounds.SizeY, modelBounds.SizeZ );
					if( m_maxModelDiagonal < modelDiagonal.Length )
						m_maxModelDiagonal = modelDiagonal.Length;

					Point3D modelCenter = modelBounds.Location + ( modelDiagonal * 0.5 );
					Vector3D expVector = modelCenter - m_explosionPoint;

					m_explodeData[ model ] = new ExplodeData
					{
						ExplodeVector = expVector,
						ModelDiagonal = modelDiagonal.Length
					};
				}
			}

			var explodeVector = explodeData.ExplodeVector;
			explodeVector.Normalize();

			var ratio = m_maxModelDiagonal - explodeData.ModelDiagonal;
			delta *= ratio;
			explodeVector *= delta;

			var translateTransform = new TranslateTransform3D( explodeVector );
			var m1 = translateTransform.Value;
			var m2 = transform.Value;
			m1.Prepend( m2 );

			model.Transform = new MatrixTransform3D( m1 );
		}

		public void Reset()
		{
			lock( this )
			{
				m_target.TraverseVisuals<Visual3D>( ( m, t ) => Reset( m ) );
			}
			m_explodeData.Clear();
		}
		private void Reset( Visual3D model )
		{
			if( model == null )
				return;

			Transform3D transform;
			if( !m_originalTransforms.TryGetValue( model, out transform ) )
				return;

			model.Transform = transform;
		}

		private struct ExplodeData
		{
			public Vector3D ExplodeVector { get; set; }
			public double ModelDiagonal { get; set; }

		}
	}
}
