using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class GridLines : ChangeableMeshModelVisual3D
	{
		/// <summary>
		/// Координаты центра сетки.
		/// </summary>
		public Point3D Center
		{
			get => ( Point3D )this.GetValue( CenterProperty );
			set => this.SetValue( CenterProperty, value );
		}
		public static readonly DependencyProperty CenterProperty;



		/// <summary>
		/// Нормаль плоскости сетки.
		/// </summary>
		public Vector3D Normal
		{
			get => ( Vector3D )this.GetValue( NormalProperty );
			set => this.SetValue( NormalProperty, value );
		}
		public static readonly DependencyProperty NormalProperty;



		/// <summary>
		/// Высота сетки.
		/// </summary>
		public double Height
		{
			get => ( double )this.GetValue( HeightProperty );
			set => this.SetValue( HeightProperty, value );
		}
		public static readonly DependencyProperty HeightProperty;



		/// <summary>
		/// Ширина сетки.
		/// </summary>
		public double Width
		{
			get => ( double )this.GetValue( WidthProperty );
			set => this.SetValue( WidthProperty, value );
		}
		public static readonly DependencyProperty WidthProperty;



		/// <summary>
		/// Расстояние между крупными клетками.
		/// </summary>
		public double LargeCellSize
		{
			get => ( double )this.GetValue( LargeCellSizeProperty );
			set => this.SetValue( LargeCellSizeProperty, value );
		}
		public static readonly DependencyProperty LargeCellSizeProperty;



		/// <summary>
		/// Расстояние между маленькими клетками.
		/// </summary>
		public double SmallCellSize
		{
			get => ( double )this.GetValue( SmallCellSizeProperty );
			set => this.SetValue( SmallCellSizeProperty, value );
		}
		public static readonly DependencyProperty SmallCellSizeProperty;



		/// <summary>
		/// Толщина линий сетки.
		/// </summary>
		public double Thickness
		{
			get => ( double )this.GetValue( ThicknessProperty );
			set => this.SetValue( ThicknessProperty, value );
		}
		public static readonly DependencyProperty ThicknessProperty;


		static GridLines()
		{
			CenterProperty = DependencyProperty.Register(
				nameof( Center ),
				typeof( Point3D ),
				typeof( GridLines ),
				new UIPropertyMetadata( new Point3D(), GeometryChangedCallback ) );

			NormalProperty = DependencyProperty.Register(
				nameof( Normal ),
				typeof( Vector3D ),
				typeof( GridLines ),
				new UIPropertyMetadata( new Vector3D( 0, 0, 1 ), GeometryChangedCallback ) );

			HeightProperty = DependencyProperty.Register(
				nameof( Height ),
				typeof( double ),
				typeof( GridLines ),
				new PropertyMetadata( 10.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			WidthProperty = DependencyProperty.Register(
				nameof( Width ),
				typeof( double ),
				typeof( GridLines ),
				new PropertyMetadata( 10.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			LargeCellSizeProperty = DependencyProperty.Register(
				nameof( LargeCellSize ),
				typeof( double ),
				typeof( GridLines ),
				new PropertyMetadata( 4.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			SmallCellSizeProperty = DependencyProperty.Register(
				nameof( SmallCellSize ),
				typeof( double ),
				typeof( GridLines ),
				new PropertyMetadata( 1.0, GeometryChangedCallback ),
				ValidateSizeValueCallback );

			ThicknessProperty = DependencyProperty.Register(
				nameof( Thickness ),
				typeof( double ),
				typeof( GridLines ),
				new PropertyMetadata( 0.08, GeometryChangedCallback ),
				ValidateSizeValueCallback );
		}
		private static bool ValidateSizeValueCallback( object value )
		{
			var scaleValue = ( double )value;
			return scaleValue > 0;
		}

		protected override MeshGeometry3D BuildMesh()
		{
			var heightDirection = this.Normal.GetPerpendicular();

			var rotateTransform = new RotateTransform3D( new AxisAngleRotation3D( this.Normal, 90.0 ) );
			var widthDirection = rotateTransform.Transform( heightDirection );
			widthDirection.Normalize();

			using( var meshBuilder = new MeshBuilder( true, false ) )
			{
				double minX = -this.Width / 2;
				double minY = -this.Height / 2;
				double maxX = this.Width / 2;
				double maxY = this.Height / 2;

				double eps = this.SmallCellSize / 10;

				double x = 0;
				while( x < maxX + eps )
				{
					double t = this.Thickness;
					if( x % this.LargeCellSize < 1e-3 )
						t *= 2;

					this.AddHeightLine( meshBuilder, x, minY, maxY, t, heightDirection, widthDirection );
					this.AddHeightLine( meshBuilder, -x, minY, maxY, t, heightDirection, widthDirection );
					x += this.SmallCellSize;
				}

				double y = 0;
				while( y < maxY + eps )
				{
					double t = this.Thickness;
					if( y % this.LargeCellSize < 1e-3 )
						t *= 2;

					this.AddWidthLine( meshBuilder, y, minX, maxX, t, heightDirection, widthDirection );
					this.AddWidthLine( meshBuilder, -y, minX, maxX, t, heightDirection, widthDirection );
					y += this.SmallCellSize;
				}

				//double minX = -this.Width / 2;
				//double minY = -this.Height / 2;
				//double maxX = this.Width / 2;
				//double maxY = this.Height / 2;

				//double x = minX;
				//double eps = this.SmallCellSize / 10;
				//while( x <= maxX + eps )
				//{
				//	double t = this.Thickness;
				//	if( IsMultipleOf( x, this.LargeCellSize ) )
				//	{
				//		t *= 2;
				//	}

				//	this.AddHeightLine( meshBuilder, x, minY, maxY, t, heightDirection, widthDirection );
				//	x += this.SmallCellSize;
				//}

				//double y = minY;
				//while( y <= maxY + eps )
				//{
				//	double t = this.Thickness;
				//	if( IsMultipleOf( y, this.LargeCellSize ) )
				//	{
				//		t *= 2;
				//	}

				//	this.AddWidthLine( meshBuilder, y, minX, maxX, t, heightDirection, widthDirection );
				//	y += this.SmallCellSize;
				//}

				return meshBuilder.ToMesh();
			}
		}

		/// <summary>
		/// Определяет, делится ли y на d.
		/// </summary>
		private static bool IsMultipleOf( double y, double d )
		{
			if( y < 0 )
				y *= -1;

			//double y2 = d * ( int )( y / d );
			//var t = Math.Abs( y - y2 );

			return y % d < 1e-2;
		}

		/// <summary>
		/// Добавление линии по оси X.
		/// </summary>
		private void AddHeightLine( MeshBuilder meshBuilder, double x, double minY, double maxY, double thickness, Vector3D heightDirection, Vector3D widthDirection )
		{
			int i0 = meshBuilder.Positions.Count;
			meshBuilder.Positions.Add( this.GetPoint( x - ( thickness / 2 ), minY, heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( x - ( thickness / 2 ), maxY, heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( x + ( thickness / 2 ), maxY, heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( x + ( thickness / 2 ), minY, heightDirection, widthDirection ) );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.TriangleIndices.Add( i0 );
			meshBuilder.TriangleIndices.Add( i0 + 1 );
			meshBuilder.TriangleIndices.Add( i0 + 2 );
			meshBuilder.TriangleIndices.Add( i0 + 2 );
			meshBuilder.TriangleIndices.Add( i0 + 3 );
			meshBuilder.TriangleIndices.Add( i0 );
		}

		/// <summary>
		/// Добавление линии по оси Y.
		/// </summary>
		private void AddWidthLine( MeshBuilder meshBuilder, double y, double minX, double maxX, double thickness, Vector3D heightDirection, Vector3D widthDirection )
		{
			int i0 = meshBuilder.Positions.Count;
			meshBuilder.Positions.Add( this.GetPoint( minX, y + ( thickness / 2 ), heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( maxX, y + ( thickness / 2 ), heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( maxX, y - ( thickness / 2 ), heightDirection, widthDirection ) );
			meshBuilder.Positions.Add( this.GetPoint( minX, y - ( thickness / 2 ), heightDirection, widthDirection ) );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.Normals.Add( this.Normal );
			meshBuilder.TriangleIndices.Add( i0 );
			meshBuilder.TriangleIndices.Add( i0 + 1 );
			meshBuilder.TriangleIndices.Add( i0 + 2 );
			meshBuilder.TriangleIndices.Add( i0 + 2 );
			meshBuilder.TriangleIndices.Add( i0 + 3 );
			meshBuilder.TriangleIndices.Add( i0 );
		}

		/// <summary>
		/// Получение точки на плоскости.
		/// </summary>
		private Point3D GetPoint( double x, double y, Vector3D heightDirection, Vector3D widthDirection )
		{
			return this.Center + ( widthDirection * x ) + ( heightDirection * y );
		}
	}
}
