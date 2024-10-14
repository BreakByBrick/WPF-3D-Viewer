using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.Viewer3D.Visuals
{
	public interface IRenderable
	{
		void SuspendRender();
		void ResumeRender( bool forceUpdate );
	}
}
