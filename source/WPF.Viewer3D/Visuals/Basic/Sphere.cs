using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class Sphere : ChangeableMeshModelVisual3D
	{
		private const ushort MIN_DIVISION_NUMBER = 3;
		private const ushort MAX_DIVISION_NUMBER = 30;


		/// <summary>
		/// Центр сферы.
		/// </summary>
		public Point3D Center
		{
			get => ( Point3D )this.GetValue( CenterProperty );
			set => this.SetValue( CenterProperty, value );
		}
		public static readonly DependencyProperty CenterProperty;


		/// <summary>
		/// Радиус сферы.
		/// </summary>
		public double Radius
		{
			get => ( double )this.GetValue( RadiusProperty );
			set => this.SetValue( RadiusProperty, value );
		}
		public static readonly DependencyProperty RadiusProperty;


		/// <summary>
		/// Количество вертикальных делений полигональной сетки, образующих сферическую поверхность.
		/// </summary>
		public ushort VerticalDivisionNumber
		{
			get => ( ushort )this.GetValue( VerticalDivisionNumberProperty );
			set => this.SetValue( VerticalDivisionNumberProperty, value );
		}
		public static readonly DependencyProperty VerticalDivisionNumberProperty;


		/// <summary>
		/// Количество горизонтальных делений полигональной сетки, образующих сферическую поверхность.
		/// </summary>
		public ushort HorizontalDivisionNumber
		{
			get => ( ushort )this.GetValue( HorizontalDivisionNumberProperty );
			set => this.SetValue( HorizontalDivisionNumberProperty, value );
		}
		public static readonly DependencyProperty HorizontalDivisionNumberProperty;


		static Sphere()
		{
			CenterProperty = DependencyProperty.Register(
				nameof( Center ),
				typeof( Point3D ),
				typeof( Sphere ),
				new PropertyMetadata( new Point3D( 0, 0, 0 ), GeometryChangedCallback ) );

			RadiusProperty = DependencyProperty.Register(
				nameof( Radius ),
				typeof( double ),
				typeof( Sphere ),
				new PropertyMetadata( 1.0, GeometryChangedCallback ),
				ValidateRadiusCallback );

			VerticalDivisionNumberProperty = DependencyProperty.Register(
				nameof( VerticalDivisionNumber ),
				typeof( ushort ),
				typeof( Sphere ),
				new PropertyMetadata( ( ushort )30, GeometryChangedCallback ),
				ValidateDivisionNumberCallback );

			HorizontalDivisionNumberProperty = DependencyProperty.Register(
				nameof( HorizontalDivisionNumber ),
				typeof( ushort ),
				typeof( Sphere ),
				new PropertyMetadata( ( ushort )30, GeometryChangedCallback ),
				ValidateDivisionNumberCallback );
		}
		private static bool ValidateDivisionNumberCallback( object value )
		{
			var divisionNumber = ( ushort )value;
			return ( divisionNumber >= MIN_DIVISION_NUMBER )
				&& ( divisionNumber <= MAX_DIVISION_NUMBER );
		}
		private static bool ValidateRadiusCallback( object value )
		{
			var radius = ( double )value;
			return radius > 0;
		}


		protected override MeshGeometry3D BuildMesh()
		{
			using( var builder = new MeshBuilder( true, true ) )
			{
				builder.AddSphere( this.Center, this.Radius, this.VerticalDivisionNumber, this.HorizontalDivisionNumber );
				return builder.ToMesh();
			}
		}
	}
}
