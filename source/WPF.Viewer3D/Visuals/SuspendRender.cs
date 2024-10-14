using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.Viewer3D.Visuals
{
	public class SuspendRender : IDisposable
	{
		private IRenderable m_target;
		private bool m_forceUpdate;

		public SuspendRender( IRenderable target ) : this( target, true ) { }
		public SuspendRender( IRenderable target, bool forceUpdate )
		{
			if( target == null )
				throw new ArgumentNullException( nameof( target ) );

			m_target = target;
			m_forceUpdate = forceUpdate;

			target.SuspendRender();
		}

		public void Dispose()
		{
			if( m_target != null )
			{
				m_target.ResumeRender( m_forceUpdate );
			}
			GC.SuppressFinalize( this );
		}
	}
}
