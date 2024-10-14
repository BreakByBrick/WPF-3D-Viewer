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
	public class Arrow : ChangeableMeshModelVisual3D
	{
		private const ushort MIN_DIVISION_NUMBER = 3;
		private const ushort MAX_DIVISION_NUMBER = 30;


		/// <summary>
		/// Координаты начала стрелки.
		/// </summary>
		public Point3D FromPoint
		{
			get => ( Point3D )this.GetValue( FromPointProperty );
			set => this.SetValue( FromPointProperty, value );
		}
		public static readonly DependencyProperty FromPointProperty;


		/// <summary>
		/// Координаты конца стрелки.
		/// </summary>
		public Point3D ToPoint
		{
			get => ( Point3D )this.GetValue( ToPointProperty );
			set => this.SetValue( ToPointProperty, value );
		}
		public static readonly DependencyProperty ToPointProperty;


		/// <summary>
		/// Диаметр стрелки.
		/// </summary>
		public double Diameter
		{
			get => ( double )this.GetValue( DiameterProperty );
			set => this.SetValue( DiameterProperty, value );
		}
		public static readonly DependencyProperty DiameterProperty;


		/// <summary>
		/// Длина наконечника стрелки (относительно диаметра).
		/// </summary>
		public double HeadLength
		{
			get => ( double )this.GetValue( HeadLengthProperty );
			set => this.SetValue( HeadLengthProperty, value );
		}
		public static readonly DependencyProperty HeadLengthProperty;


		/// <summary>
		/// Количество делений полигональной сетки, образующей тело стрелки.
		/// </summary>
		public ushort DivisionNumber
		{
			get => ( ushort )this.GetValue( DivisionNumberProperty );
			set => this.SetValue( DivisionNumberProperty, value );
		}
		public static readonly DependencyProperty DivisionNumberProperty;


		static Arrow()
		{
			FromPointProperty = DependencyProperty.Register(
				nameof( FromPoint ),
				typeof( Point3D ),
				typeof( Arrow ),
				new PropertyMetadata( new Point3D( 0, 0, 0 ), GeometryChangedCallback ) );

			ToPointProperty = DependencyProperty.Register(
				nameof( ToPoint ),
				typeof( Point3D ),
				typeof( Arrow ),
				new PropertyMetadata( new Point3D( 0, 0, 10 ), GeometryChangedCallback ) );

			DiameterProperty = DependencyProperty.Register(
				nameof( Diameter ),
				typeof( double ),
				typeof( Arrow ),
				new PropertyMetadata( 1.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			HeadLengthProperty = DependencyProperty.Register(
				nameof( HeadLength ),
				typeof( double ),
				typeof( Arrow ),
				new PropertyMetadata( 3.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			DivisionNumberProperty = DependencyProperty.Register(
				nameof( DivisionNumber ),
				typeof( ushort ),
				typeof( Arrow ),
				new PropertyMetadata( ( ushort )30, GeometryChangedCallback ),
				ValidateDivisionNumberCallback );
		}
		private static bool ValidateSizeValueCallback( object value )
		{
			var scaleValue = ( double )value;
			return scaleValue > 0;
		}
		private static bool ValidateDivisionNumberCallback( object value )
		{
			var divisionNumber = ( ushort )value;
			return ( divisionNumber >= MIN_DIVISION_NUMBER )
				&& ( divisionNumber <= MAX_DIVISION_NUMBER );
		}


		protected override MeshGeometry3D BuildMesh()
		{
			using( var builder = new MeshBuilder( true, true ) )
			{
				builder.AddArrow( FromPoint, ToPoint, Diameter, HeadLength, DivisionNumber );
				return builder.ToMesh();
			}
		}
	}
}
