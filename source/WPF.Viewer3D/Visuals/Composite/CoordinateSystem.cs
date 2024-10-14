using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class CoordinateSystem : ChangeableModelVisual3D
	{
		private readonly Point3D m_coordinateCenter = new Point3D( 0, 0, 0 );

		private Sphere m_origin;
		private readonly Dictionary<Axis, Arrow> m_arrows = new Dictionary<Axis, Arrow>();
		private readonly Dictionary<Axis, BillboardText> m_textBlocks = new Dictionary<Axis, BillboardText>();

		private bool m_isGeometryChanged = true;
		private bool m_isOriginColorChanged = true;
		private bool m_isTextColorChanged = true;
		private Axis m_axisColorChangedMask = Axis.X | Axis.Y | Axis.Z;

		public Color FontColor
		{
			get { return ( Color )this.GetValue( FontColorProperty ); }
			set { this.SetValue( FontColorProperty, value ); }
		}
		public static readonly DependencyProperty FontColorProperty;


		public Color OriginColor
		{
			get { return ( Color )this.GetValue( OriginColorProperty ); }
			set { this.SetValue( OriginColorProperty, value ); }
		}
		public static readonly DependencyProperty OriginColorProperty;


		public Color XAxisColor
		{
			get { return ( Color )this.GetValue( XAxisColorProperty ); }
			set { this.SetValue( XAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty XAxisColorProperty;


		private Color YAxisColor
		{
			get { return ( Color )this.GetValue( YAxisColorProperty ); }
			set { this.SetValue( YAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty YAxisColorProperty;


		private Color ZAxisColor
		{
			get { return ( Color )this.GetValue( ZAxisColorProperty ); }
			set { this.SetValue( ZAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty ZAxisColorProperty;


		public double DiagonalLength
		{
			get { return ( double )GetValue( DiagonalLengthProperty ); }
			set { SetValue( DiagonalLengthProperty, value ); }
		}
		public static readonly DependencyProperty DiagonalLengthProperty;


		public Matrix3D CameraMatrix
		{
			get { return ( Matrix3D )GetValue( CameraMatrixProperty ); }
			set { SetValue( CameraMatrixProperty, value ); }
		}
		public static readonly DependencyProperty CameraMatrixProperty;


		static CoordinateSystem()
		{
			FontColorProperty = DependencyProperty.Register(
				nameof( FontColor ),
				typeof( Color ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Colors.Black, TextColorChangedCallback ) );

			OriginColorProperty = DependencyProperty.Register(
				nameof( OriginColor ),
				typeof( Color ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Colors.Yellow, OriginColorChanedCallback ) );

			XAxisColorProperty = DependencyProperty.Register(
				nameof( XAxisColor ),
				typeof( Color ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Colors.Red, ( d, e ) => AxisColorChangedCallback( d, e, Axis.X ) ) );

			YAxisColorProperty = DependencyProperty.Register(
				nameof( YAxisColor ),
				typeof( Color ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Colors.Blue, ( d, e ) => AxisColorChangedCallback( d, e, Axis.Y ) ) );

			ZAxisColorProperty = DependencyProperty.Register(
				nameof( ZAxisColor ),
				typeof( Color ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Colors.Green, ( d, e ) => AxisColorChangedCallback( d, e, Axis.Z ) ) );

			DiagonalLengthProperty = DependencyProperty.Register(
				nameof( DiagonalLength ),
				typeof( double ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( 100.0, DiagonalLengthChangedCallback ) );

			CameraMatrixProperty = DependencyProperty.Register(
				nameof( CameraMatrix ),
				typeof( Matrix3D ),
				typeof( CoordinateSystem ),
				new PropertyMetadata( Matrix3D.Identity ) );
		}

		private static void AxisColorChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e, Axis axisColorChangedMask )
		{
			var coordinateSystem = ( CoordinateSystem )d;
			coordinateSystem.m_axisColorChangedMask |= axisColorChangedMask;
			ChangedCallback( d, e );
		}
		private static void OriginColorChanedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var coordinateSystem = ( CoordinateSystem )d;
			coordinateSystem.m_isOriginColorChanged = true;
			ChangedCallback( d, e );
		}
		private static void TextColorChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var coordinateSystem = ( CoordinateSystem )d;
			coordinateSystem.m_isTextColorChanged = true;
			ChangedCallback( d, e );
		}
		private static void DiagonalLengthChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var coordinateSystem = ( CoordinateSystem )d;
			coordinateSystem.m_isGeometryChanged = true;
			ChangedCallback( d, e );
		}

		public CoordinateSystem()
		{
			UpdateModel();
		}


		protected override void UpdateModel()
		{
			UpdateGeometry();
			UpdateArrowColors();
			UpdateOriginColor();
			UpdateTextColor();
		}


		private void UpdateGeometry()
		{
			if( !m_isGeometryChanged )
				return;

			// Вращение системы координат выполняется от начала осей.
			// Таким образом длина стороны условного куба, в который должна помещаться Система координат 
			// должна быть равна длина_стрелки_оси_координат * 2.

			// Диагональ модели Системы координат (расстояние между максимально удаленными точками) таким образом будет равна гипотенузе 
			// прямоугольного треугольника, в котором длина катетов равна длине стороны условного куба, 
			// в который помещается Система координат.

			// Расчет стороны условного куба, в который помещается Система координат: a = √((c^2)/2),
			// где a - длина стороны условного куба, в который помещается Система координат,
			// c - диагональ условного куба, в который помещается Система координат.

			// За диагональ модели Системы координат возьмем сторону области просмотра Системы координат.

			var a = Math.Sqrt( Math.Pow( DiagonalLength, 2 ) / 2 );

			// Длина стрелки системы координат равна половине длины стороны условного куба, в который поомещается Система координат.
			var arrowLength = a / 2;

			var textBlockHeight = DiagonalLength / 10; // 10.0;
			var textBlockWidth = DiagonalLength / 20; // 5.0;

			double billboardDistance = arrowLength - textBlockHeight / 2;
			arrowLength = billboardDistance * 0.9;
			double diameter = arrowLength * 0.1;



			var arrow = GetArrow( Axis.X );
			using( new SuspendRender( arrow ) )
			{
				arrow.FromPoint = m_coordinateCenter;
				arrow.ToPoint = GetPointOnAxis( Axis.X, arrowLength );
				arrow.Diameter = diameter;
			}


			arrow = GetArrow( Axis.Y );
			using( new SuspendRender( arrow ) )
			{
				arrow.FromPoint = m_coordinateCenter;
				arrow.ToPoint = GetPointOnAxis( Axis.Y, arrowLength );
				arrow.Diameter = diameter;
			}


			arrow = GetArrow( Axis.Z );
			using( new SuspendRender( arrow ) )
			{
				arrow.FromPoint = m_coordinateCenter;
				arrow.ToPoint = GetPointOnAxis( Axis.Z, arrowLength );
				arrow.Diameter = diameter;
			}


			var origin = GetOrigin();
			using( new SuspendRender( origin ) )
			{
				origin.Center = m_coordinateCenter;
				origin.Radius = diameter;
			}


			var cameraDirectionText = GetTextBlock( Axis.X );
			using( new SuspendRender( cameraDirectionText ) )
			{
				cameraDirectionText.Center = GetPointOnAxis( Axis.X, billboardDistance );
				cameraDirectionText.Width = textBlockWidth;
				cameraDirectionText.Height = textBlockHeight;
			}


			cameraDirectionText = GetTextBlock( Axis.Y );
			using( new SuspendRender( cameraDirectionText ) )
			{
				cameraDirectionText.Center = GetPointOnAxis( Axis.Y, billboardDistance );
				cameraDirectionText.Width = textBlockWidth;
				cameraDirectionText.Height = textBlockHeight;
			}


			cameraDirectionText = GetTextBlock( Axis.Z );
			using( new SuspendRender( cameraDirectionText ) )
			{
				cameraDirectionText.Center = GetPointOnAxis( Axis.Z, billboardDistance );
				cameraDirectionText.Width = textBlockWidth;
				cameraDirectionText.Height = textBlockHeight;
			}
		}
		private Sphere GetOrigin()
		{
			if( m_origin == null )
			{
				m_origin = new Sphere();
				this.Children.Add( m_origin );
			}
			return m_origin;
		}
		private Arrow GetArrow( Axis axis )
		{
			if( m_arrows.ContainsKey( axis ) )
			{
				var arrow = m_arrows[ axis ];
				if( arrow != null )
				{
					return arrow;
				}
			}
			return CreateArrow( axis );
		}
		private Arrow CreateArrow( Axis axis )
		{
			var arrow = new Arrow();

			m_arrows[ axis ] = arrow;
			this.Children.Add( arrow );

			return arrow;
		}
		private BillboardText GetTextBlock( Axis axis )
		{
			if( m_textBlocks.ContainsKey( axis ) )
			{
				var textBlock = m_textBlocks[ axis ];
				if( textBlock != null )
				{
					return textBlock;
				}
			}
			return CreateTextBlock( axis );
		}
		private BillboardText CreateTextBlock( Axis axis )
		{
			var textBlock = new BillboardText();
			textBlock.Text = GetText( axis );
			BindingOperations.SetBinding( textBlock, BillboardText.CameraMatrixProperty, new Binding()
			{
				Source = this,
				Path = new PropertyPath( nameof( CameraMatrix ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			m_textBlocks[ axis ] = textBlock;
			this.Children.Add( textBlock );

			return textBlock;
		}
		private string GetText( Axis axis )
		{
			switch( axis )
			{
				case Axis.X:
					return @"X";
				case Axis.Y:
					return @"Y";
				case Axis.Z:
					return @"Z";
				default:
					return String.Empty;
			}
		}
		private Point3D GetPointOnAxis( Axis axis, double distance )
		{
			switch( axis )
			{
				case Axis.X:
					return new Point3D( distance, 0, 0 );
				case Axis.Y:
					return new Point3D( 0, distance, 0 );
				case Axis.Z:
					return new Point3D( 0, 0, distance );
				default:
					return new Point3D();
			}
		}


		private void UpdateOriginColor()
		{
			if( !m_isOriginColorChanged )
				return;

			m_origin.Material = MaterialHelper.CreateMaterial( OriginColor );
		}


		private void UpdateArrowColors()
		{
			if( m_axisColorChangedMask == 0 )
				return;

			foreach( Axis axis in Enum.GetValues( typeof( Axis ) ) )
			{
				if( ( m_axisColorChangedMask & axis ) != 0 )
				{
					UpdateArrowColor( axis );
					m_axisColorChangedMask &= ~axis;
				}
			}
		}
		private void UpdateArrowColor( Axis axis )
		{
			var arrow = GetArrow( axis );
			var color = GetArrowColor( axis );
			arrow.Material = MaterialHelper.CreateMaterial( color );
		}
		private Color GetArrowColor( Axis axis )
		{
			switch( axis )
			{
				case Axis.X:
					return XAxisColor;
				case Axis.Y:
					return YAxisColor;
				case Axis.Z:
					return ZAxisColor;
				default:
					return Colors.Gray;
			}
		}


		private void UpdateTextColor()
		{
			if( !m_isTextColorChanged )
				return;

			foreach( var textBlock in m_textBlocks.Values )
				textBlock.FontColor = FontColor;
		}


		[Flags]
		private enum Axis : byte
		{
			None = 0,
			X = 1,
			Y = 1 << 1,
			Z = 1 << 2
		}
	}
}
