using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WPF.Viewer3D
{
	internal static class MouseHelper
	{
		public static Point GetPosition( Visual relativeTo )
		{
			return relativeTo.PointFromScreen( GetPosition() );
		}

		public static Point GetPosition()
		{
			Win32Point w32Mouse = new Win32Point();
			GetCursorPos( ref w32Mouse );
			return new Point( w32Mouse.X, w32Mouse.Y );
		}

		public static void SetPosition( Point pt )
		{
			SetPosition( pt.X, pt.Y );
		}

		public static void SetPosition( double x, double y )
		{
			SetCursorPos( ( int )x, ( int )y );
		}

		[StructLayout( LayoutKind.Sequential )]
		internal struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		};

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		internal static extern bool GetCursorPos( ref Win32Point pt );

		[System.Runtime.InteropServices.DllImportAttribute( "user32.dll", EntryPoint = "SetCursorPos" )]
		[return: System.Runtime.InteropServices.MarshalAsAttribute( System.Runtime.InteropServices.UnmanagedType.Bool )]
		internal static extern bool SetCursorPos( int x, int y );
	}
}
