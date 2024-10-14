using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class BillboardText : ChangeableModelVisual3D
	{
		private bool m_isMaterialChanged = true;
		private bool m_isMeshChanged = true;

		private MeshGeometry3D m_geometry;
		private GeometryModel3D m_model;

		private double LeftOffset => -0.5 * Width;
		private double RightOffset => 0.5 * Width;
		private double TopOffset => -0.5 * Height;
		private double BottomOffset => 0.5 * Height;


		/// <summary>
		/// Координаты центра блока с текстом.
		/// </summary>
		public Point3D Center
		{
			get { return ( Point3D )GetValue( CenterProperty ); }
			set { SetValue( CenterProperty, value ); }
		}
		public static readonly DependencyProperty CenterProperty;


		/// <summary>
		/// Высота блока с текстом.
		/// </summary>
		public double Height
		{
			get { return ( double )GetValue( HeightProperty ); }
			set { SetValue( HeightProperty, value ); }
		}
		public static readonly DependencyProperty HeightProperty;


		/// <summary>
		/// Ширина блока с текстом.
		/// </summary>
		public double Width
		{
			get { return ( double )GetValue( WidthProperty ); }
			set { SetValue( WidthProperty, value ); }
		}
		public static readonly DependencyProperty WidthProperty;


		/// <summary>
		/// Текст.
		/// </summary>
		public string Text
		{
			get { return ( string )GetValue( TextProperty ); }
			set { SetValue( TextProperty, value ); }
		}
		public static readonly DependencyProperty TextProperty;


		/// <summary>
		/// Семейство шрифтов.
		/// </summary>
		public FontFamily FontFamily
		{
			get { return ( FontFamily )GetValue( FontFamilyProperty ); }
			set { SetValue( FontFamilyProperty, value ); }
		}
		public static readonly DependencyProperty FontFamilyProperty;


		/// <summary>
		/// Размер шрифта.
		/// </summary>
		public double FontSize
		{
			get { return ( double )GetValue( FontSizeProperty ); }
			set { SetValue( FontSizeProperty, value ); }
		}
		public static readonly DependencyProperty FontSizeProperty;


		/// <summary>
		/// Толщина шрифта.
		/// </summary>
		public FontWeight FontWeight
		{
			get { return ( FontWeight )GetValue( FontWeightProperty ); }
			set { SetValue( FontWeightProperty, value ); }
		}
		public static readonly DependencyProperty FontWeightProperty;


		/// <summary>
		/// Цвет текста.
		/// </summary>
		public Color FontColor
		{
			get { return ( Color )GetValue( FontColorProperty ); }
			set { SetValue( FontColorProperty, value ); }
		}
		public static readonly DependencyProperty FontColorProperty;


		/// <summary>
		/// Матрица обзора камеры.
		/// </summary>
		public Matrix3D CameraMatrix
		{
			get { return ( Matrix3D )GetValue( CameraMatrixProperty ); }
			set { SetValue( CameraMatrixProperty, value ); }
		}
		public static readonly DependencyProperty CameraMatrixProperty;


		static BillboardText()
		{
			CenterProperty = DependencyProperty.Register(
				nameof( Center ),
				typeof( Point3D ),
				typeof( BillboardText ),
				new PropertyMetadata( new Point3D(), MeshChanged ) );

			WidthProperty = DependencyProperty.Register(
				nameof( Width ),
				typeof( double ),
				typeof( BillboardText ),
				new PropertyMetadata( 5.0, MeshChanged ),
				ValidateSizeValueCallback );

			HeightProperty = DependencyProperty.Register(
				nameof( Height ),
				typeof( double ),
				typeof( BillboardText ),
				new PropertyMetadata( 10.0, MeshChanged ),
				ValidateSizeValueCallback );

			TextProperty = DependencyProperty.Register(
				nameof( Text ),
				typeof( string ),
				typeof( BillboardText ),
				new PropertyMetadata( "Text", MaterialChangedCallback ) );

			FontFamilyProperty = DependencyProperty.Register(
				nameof( FontFamily ),
				typeof( FontFamily ),
				typeof( BillboardText ),
				new PropertyMetadata( new FontFamily( "Calibri" ), MaterialChangedCallback ) );

			FontSizeProperty = DependencyProperty.Register(
				nameof( FontSize ),
				typeof( double ),
				typeof( BillboardText ),
				new PropertyMetadata( 18.0, MaterialChangedCallback ),
				ValidateSizeValueCallback );

			FontWeightProperty = DependencyProperty.Register(
				nameof( FontWeight ),
				typeof( FontWeight ),
				typeof( BillboardText ),
				new PropertyMetadata( FontWeights.Normal, MaterialChangedCallback ) );

			FontColorProperty = DependencyProperty.Register(
				nameof( FontColor ),
				typeof( Color ),
				typeof( BillboardText ),
				new PropertyMetadata( Colors.Black, MaterialChangedCallback ) );

			CameraMatrixProperty = DependencyProperty.Register(
				nameof( CameraMatrix ),
				typeof( Matrix3D ),
				typeof( BillboardText ),
				new PropertyMetadata( Matrix3D.Identity, MeshChanged ),
				ValidateCameraMatrixCallback );
		}
		private static bool ValidateSizeValueCallback( object value )
		{
			var sizeValue = ( double )value;
			return ( sizeValue > 0 );
		}
		private static bool ValidateCameraMatrixCallback( object value )
		{
			var cameraMatrix = ( Matrix3D )value;

			if( double.IsNaN( cameraMatrix.M11 ) )
				return false;

			if( !cameraMatrix.HasInverse )
				return false;

			return true;
		}
		private static void MeshChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var billboardText = ( BillboardText )d;
			billboardText.m_isMeshChanged = true;
			ChangedCallback( d, e );
		}
		private static void MaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var billboardText = ( BillboardText )d;
			billboardText.m_isMaterialChanged = true;
			ChangedCallback( d, e );
		}

		public BillboardText()
		{
			#region TriangleIndices
			var triangleIndices = new Int32Collection( 6 );

			// нижний правый треугольник
			triangleIndices.Add( 0 );
			triangleIndices.Add( 1 );
			triangleIndices.Add( 2 );

			// верхний левый треугольник
			triangleIndices.Add( 2 );
			triangleIndices.Add( 3 );
			triangleIndices.Add( 0 );

			triangleIndices.Freeze();
			#endregion

			#region TextureCoordinates
			var textureCoordinates = new PointCollection( 4 );

			textureCoordinates.Add( new Point( 0, 0 ) );
			textureCoordinates.Add( new Point( 1, 0 ) );
			textureCoordinates.Add( new Point( 1, 1 ) );
			textureCoordinates.Add( new Point( 0, 1 ) );

			textureCoordinates.Freeze();
			#endregion

			m_geometry = new MeshGeometry3D();
			m_geometry.TriangleIndices = triangleIndices;
			m_geometry.TextureCoordinates = textureCoordinates;

			m_model = new GeometryModel3D();
			m_model.Geometry = m_geometry;

			this.Content = m_model;
		}

		protected override void UpdateModel()
		{
			UpdateMesh();
			UpdateMaterial();
		}
		private void UpdateMesh()
		{
			if( !m_isMeshChanged )
				return;

			var transformMatrix = CameraMatrix;
			var modelToScreen = transformMatrix;
			transformMatrix.Invert();

			m_geometry.Positions = GetCornerPositions( modelToScreen, transformMatrix );
		}
		private Point3DCollection GetCornerPositions( Matrix3D modelToScreen, Matrix3D screenToModel )
		{
			var positions = new Point3DCollection( 4 );

			Point3D positionOnScreen = Center * modelToScreen;

			double spx = positionOnScreen.X;
			double spy = positionOnScreen.Y;
			double spz = positionOnScreen.Z;

			// Далее будут рассчитаны положения углов области отображения надписи в соответствии с проекцией на плоскость области просмотра.

			var leftOffset = LeftOffset;
			var rightOffset = RightOffset;
			var topOffset = TopOffset;
			var bottomOffset = BottomOffset;

			// Рассчет позиции в нижнем левом углу.
			var p = new Point3D( spx + leftOffset, spy + bottomOffset, spz ) * screenToModel;
			positions.Add( p );

			// Рассчет позиции в нижнем правом углу.
			p = new Point3D( spx + rightOffset, spy + bottomOffset, spz ) * screenToModel;
			positions.Add( p );

			// Рассчет позиции в верхнем правом углу.
			p = new Point3D( spx + rightOffset, spy + topOffset, spz ) * screenToModel;
			positions.Add( p );

			// Рассчет позиции в верхнем левом углу.
			p = new Point3D( spx + leftOffset, spy + topOffset, spz ) * screenToModel;
			positions.Add( p );

			positions.Freeze();
			return positions;
		}
		private void UpdateMaterial()
		{
			if( !m_isMaterialChanged )
				return;

			string text = Text;
			if( string.IsNullOrEmpty( text ) )
				return;

			var textBlock = new TextBlock( new Run( text ) )
			{
				Foreground = new SolidColorBrush( FontColor ),
				Background = new SolidColorBrush( Colors.Transparent ),
				FontWeight = FontWeight,
				//Padding = m_padding
			};

			FontFamily fontFamily = FontFamily;
			if( fontFamily != null )
				textBlock.FontFamily = fontFamily;

			double fontSize = FontSize;
			if( fontSize > 0 )
				textBlock.FontSize = fontSize;

			textBlock.Measure( new Size( 1000, 1000 ) );
			textBlock.Arrange( new Rect( textBlock.DesiredSize ) );

			var renderTargetBitmap = new RenderTargetBitmap( ( int )textBlock.ActualWidth + 1, ( int )textBlock.ActualHeight + 1, 96, 96, PixelFormats.Pbgra32 );
			renderTargetBitmap.Render( textBlock );

			var brush = new ImageBrush( renderTargetBitmap );

			m_model.BackMaterial = new DiffuseMaterial( brush );
			//m_model.BackMaterial = new EmissiveMaterial( brush );
		}
	}
}
