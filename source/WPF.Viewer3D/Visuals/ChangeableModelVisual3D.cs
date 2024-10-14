using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	/// <summary>
	/// Изменяемая модель.
	/// </summary>
	public abstract class ChangeableModelVisual3D : ModelVisual3D, IRenderable
	{
		private bool m_isRenderSuspended;
		private bool m_isChanged;

		/// <summary>
		/// Обновление изменяемой модели.
		/// </summary>
		protected abstract void UpdateModel();
		private void TryUpdate()
		{
			if( !m_isRenderSuspended )
			{
				this.UpdateModel();
			}
			else
			{
				m_isChanged = true;
			}
		}

		/// <summary>
		/// Обработчик изменений модели.
		/// </summary>
		protected static void ChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			ChangeableModelVisual3D model = ( ChangeableModelVisual3D )d;
			model.TryUpdate();
		}


		/// <summary>
		/// Остановить обновление модели при изменениях.
		/// </summary>
		public void SuspendRender()
		{
			m_isRenderSuspended = true;
			m_isChanged = false;
		}
		/// <summary>
		/// Возобновить обновление модели при изменениях.
		/// </summary>
		/// <param name="forceUpdate">Принудительно обновить после возобновления.</param>
		public void ResumeRender( bool forceUpdate )
		{
			m_isRenderSuspended = false;

			if( forceUpdate )
			{
				if( m_isChanged )
				{
					TryUpdate();
				}
			}
		}
	}
}
