using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	/// <summary>
	/// Набор сечений, применяемых к заданным моделям.
	/// </summary>
	internal class CuttingBundle
	{
		/// <summary>
		/// Список для хранения порядка применения сечений.
		/// </summary>
		private List<KeyValuePair<CuttingPlane, Cutting>> m_cuttingDataList;
		private Visual3DCollection m_cutObjects;
		public CuttingBundle( Visual3DCollection cutObjects )
		{
			m_cutObjects = cutObjects;
			m_cuttingDataList = new List<KeyValuePair<CuttingPlane, Cutting>>();
		}

		/// <summary>
		/// Применяет сечение к моделям в заданной плоскости и добавляет в список порядка применения.
		/// </summary>
		public void Add( CuttingPlane plane, bool complement )
		{
			var sameCuttingData = m_cuttingDataList.FirstOrDefault( c => c.Key == plane );
			if( !sameCuttingData.Equals( default( KeyValuePair<CuttingPlane, Cutting> ) ) )
				return;

			var newCutting = new Cutting( m_cutObjects, plane, complement );
			newCutting.Apply();

			var newCuttingData = new KeyValuePair<CuttingPlane, Cutting>( plane, newCutting );
			m_cuttingDataList.Add( newCuttingData );
		}

		/// <summary>
		/// Отменяет применение сечения с заданной плоскостью и удаляет из списка порядка применения.
		/// </summary>
		public void Remove( CuttingPlane plane )
		{
			var removeCuttingData = m_cuttingDataList.FirstOrDefault( c => c.Key == plane );
			if( removeCuttingData.Equals( default( KeyValuePair<CuttingPlane, Cutting> ) ) )
				return;

			removeCuttingData.Value.Reset();

			if( m_cuttingDataList.Count > 1 )
			{
				var removeCuttingDataIndex = m_cuttingDataList.IndexOf( removeCuttingData );
				var reapplyCount = m_cuttingDataList.Count - ( removeCuttingDataIndex + 1 );
				var reapplyCuttingDataList = m_cuttingDataList.GetRange( removeCuttingDataIndex + 1, reapplyCount );

				var originalGeometry = removeCuttingData.Value.OriginalGeometryData;
				foreach( var reapplyCuttingData in reapplyCuttingDataList )
				{
					var reapplyCutting = reapplyCuttingData.Value;
					reapplyCutting.OriginalGeometryData = originalGeometry;
					reapplyCutting.Reset();
					reapplyCutting.Apply();
					originalGeometry = reapplyCutting.OriginalGeometryData;
				}
			}

			m_cuttingDataList.Remove( removeCuttingData );
		}
		public bool Contains( CuttingPlane plane )
		{
			var removeCuttingData = m_cuttingDataList.FirstOrDefault( c => c.Key == plane );
			if( removeCuttingData.Equals( default( KeyValuePair<CuttingPlane, Cutting> ) ) )
				return false;

			return true;
		}
	}
}
