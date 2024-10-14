using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class ViewCube : ChangeableModelVisual3D
	{
		private const int SIDE_TEXT_FONT_SIZE = 18;

		private readonly Point3D m_coordinateCenter = new Point3D( 0, 0, 0 );

		/// <summary>
		/// Диагональ (расстояние между самыми отдаленными точками) куба.
		/// </summary>
		private double CubeFaceLength
		{
			get
			{
				// Расчет стороны куба по длине диагонали: a = d/√3 
				// За длину диагонали куба (расстояние между самыми отдаленными точками куба) берем длину стороны области просмотра.
				return DiagonalLength / Math.Sqrt( 3 );
			}
		}

		/// <summary>
		/// Расстояние от центра до стенки куба.
		/// </summary>
		private double Size
		{
			get
			{
				return CubeFaceLength / 2;
			}
		}
		private double HalfSize
		{
			get
			{
				return Size / 2;
			}
		}
		private double QuarterSize
		{
			get
			{
				return HalfSize / 2;
			}
		}


		private Vector3D m_bottomToTopDirection;
		public Vector3D BottomToTopDirection
		{
			get
			{
				if( m_bottomToTopDirection == default( Vector3D ) )
					m_bottomToTopDirection = new Vector3D( 0, 0, 1 );

				return m_bottomToTopDirection;
			}
		}

		private Vector3D m_rightToLeftDirection;
		public Vector3D RightToLeftDirection
		{
			get
			{
				if( m_rightToLeftDirection == default( Vector3D ) )
				{
					var bottomToTopDirection = BottomToTopDirection;
					m_rightToLeftDirection = new Vector3D( bottomToTopDirection.Y, bottomToTopDirection.Z, bottomToTopDirection.X );
				}
				return m_rightToLeftDirection;
			}
		}

		private Vector3D m_backToFrontDirection;
		public Vector3D BackToFrontDirection
		{
			get
			{
				if( m_backToFrontDirection == default( Vector3D ) )
				{
					var bottomToTopDirection = BottomToTopDirection;
					var rightToLeftDirection = RightToLeftDirection;
					m_backToFrontDirection = Vector3D.CrossProduct( rightToLeftDirection, bottomToTopDirection );
				}
				return m_backToFrontDirection;
			}
		}

		/// <summary>
		/// Направления (перпиндикуляр) плоскостей составных компонентов куба.
		/// </summary>
		private readonly Dictionary<object, Vector3D> m_cubePartNormalVectors = new Dictionary<object, Vector3D>();
		/// <summary>
		/// Наклоны (вращение) плоскостей составных компонентов куба.
		/// </summary>
		private readonly Dictionary<object, Vector3D> m_cubePartUpVectors = new Dictionary<object, Vector3D>();

		private readonly Dictionary<CubeFaceType, ModelUIElement3D> m_faces = new Dictionary<CubeFaceType, ModelUIElement3D>();
		private CubeFaceType m_changedGeometryFacesMask = CubeFaceType.Front
			| CubeFaceType.Back
			| CubeFaceType.Left
			| CubeFaceType.Right
			| CubeFaceType.Top
			| CubeFaceType.Bottom;
		private CubeFaceType m_changedMaterialFacesMask = CubeFaceType.Front
			| CubeFaceType.Back
			| CubeFaceType.Left
			| CubeFaceType.Right
			| CubeFaceType.Top
			| CubeFaceType.Bottom;

		private readonly Dictionary<CubeEdgeType, ModelUIElement3D> m_edges = new Dictionary<CubeEdgeType, ModelUIElement3D>();
		private CubeEdgeType m_changedGeometryEdgesMask = CubeEdgeType.BackLeft
			| CubeEdgeType.BackRight
			| CubeEdgeType.BottomBack
			| CubeEdgeType.BottomFront
			| CubeEdgeType.BottomLeft
			| CubeEdgeType.BottomRight
			| CubeEdgeType.FrontLeft
			| CubeEdgeType.FrontRight
			| CubeEdgeType.TopBack
			| CubeEdgeType.TopFront
			| CubeEdgeType.TopLeft
			| CubeEdgeType.TopRight;
		private CubeEdgeType m_changedMaterialEdgesMask = CubeEdgeType.BackLeft
			| CubeEdgeType.BackRight
			| CubeEdgeType.BottomBack
			| CubeEdgeType.BottomFront
			| CubeEdgeType.BottomLeft
			| CubeEdgeType.BottomRight
			| CubeEdgeType.FrontLeft
			| CubeEdgeType.FrontRight
			| CubeEdgeType.TopBack
			| CubeEdgeType.TopFront
			| CubeEdgeType.TopLeft
			| CubeEdgeType.TopRight;

		private static readonly Dictionary<CubeEdgeType, Point3D> m_edgePoints = new Dictionary<CubeEdgeType, Point3D>
		{
			{ CubeEdgeType.TopFront, new Point3D(0, 1, -1) },
			{ CubeEdgeType.TopBack, new Point3D(0, 1, 1) },
			{ CubeEdgeType.TopRight, new Point3D(1, 1, 0) },
			{ CubeEdgeType.TopLeft, new Point3D(-1, 1, 0) },

			{ CubeEdgeType.BottomFront, new Point3D(0, -1, -1) },
			{ CubeEdgeType.BottomBack, new Point3D(0, -1, 1) },
			{ CubeEdgeType.BottomRight, new Point3D(1, -1, 0) },
			{ CubeEdgeType.BottomLeft, new Point3D(-1, -1, 0) },

			{ CubeEdgeType.FrontRight, new Point3D(1, 0, -1) },
			{ CubeEdgeType.FrontLeft, new Point3D(-1, 0, -1) },
			{ CubeEdgeType.BackRight, new Point3D(1, 0, 1) },
			{ CubeEdgeType.BackLeft, new Point3D(-1, 0, 1) },
		};

		private readonly Dictionary<CubeCornerType, ModelUIElement3D> m_corners = new Dictionary<CubeCornerType, ModelUIElement3D>();
		private CubeCornerType m_changedGeometryCornersMask = CubeCornerType.BottomBackLeft
			| CubeCornerType.BottomBackRight
			| CubeCornerType.BottomFrontLeft
			| CubeCornerType.BottomFrontRight
			| CubeCornerType.TopBackLeft
			| CubeCornerType.TopBackRight
			| CubeCornerType.TopFrontLeft
			| CubeCornerType.TopFrontRight;
		private CubeCornerType m_changedMaterialCornersMask = CubeCornerType.BottomBackLeft
			| CubeCornerType.BottomBackRight
			| CubeCornerType.BottomFrontLeft
			| CubeCornerType.BottomFrontRight
			| CubeCornerType.TopBackLeft
			| CubeCornerType.TopBackRight
			| CubeCornerType.TopFrontLeft
			| CubeCornerType.TopFrontRight;

		private static readonly Dictionary<CubeCornerType, Point3D> m_cornerPoints = new Dictionary<CubeCornerType, Point3D>
		{
			{ CubeCornerType.BottomFrontLeft, new Point3D(-1, -1, -1)},
			{ CubeCornerType.BottomFrontRight, new Point3D(1, -1, -1)},

			{ CubeCornerType.TopFrontRight, new Point3D(1, 1, -1)},
			{ CubeCornerType.TopFrontLeft, new Point3D(-1, 1, -1)},

			{ CubeCornerType.BottomBackLeft, new Point3D(-1, -1, 1)},
			{ CubeCornerType.BottomBackRight, new Point3D(1, -1, 1)},

			{ CubeCornerType.TopBackRight, new Point3D(1, 1, 1)},
			{ CubeCornerType.TopBackLeft, new Point3D(-1, 1, 1)},
		};


		public string FrontText
		{
			get { return ( string )this.GetValue( FrontTextProperty ); }
			set { this.SetValue( FrontTextProperty, value ); }
		}
		public static readonly DependencyProperty FrontTextProperty;


		public string BackText
		{
			get { return ( string )this.GetValue( BackTextProperty ); }
			set { this.SetValue( BackTextProperty, value ); }
		}
		public static readonly DependencyProperty BackTextProperty;


		public string LeftText
		{
			get { return ( string )this.GetValue( LeftTextProperty ); }
			set { this.SetValue( LeftTextProperty, value ); }
		}
		public static readonly DependencyProperty LeftTextProperty;


		public string RightText
		{
			get { return ( string )this.GetValue( RightTextProperty ); }
			set { this.SetValue( RightTextProperty, value ); }
		}
		public static readonly DependencyProperty RightTextProperty;


		public string TopText
		{
			get { return ( string )this.GetValue( TopTextProperty ); }
			set { this.SetValue( TopTextProperty, value ); }
		}
		public static readonly DependencyProperty TopTextProperty;


		public string BottomText
		{
			get { return ( string )this.GetValue( BottomTextProperty ); }
			set { this.SetValue( BottomTextProperty, value ); }
		}
		public static readonly DependencyProperty BottomTextProperty;


		public Color XAxisColor
		{
			get { return ( Color )GetValue( XAxisColorProperty ); }
			set { SetValue( XAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty XAxisColorProperty;


		public Color YAxisColor
		{
			get { return ( Color )GetValue( YAxisColorProperty ); }
			set { SetValue( YAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty YAxisColorProperty;


		public Color ZAxisColor
		{
			get { return ( Color )GetValue( ZAxisColorProperty ); }
			set { SetValue( ZAxisColorProperty, value ); }
		}
		public static readonly DependencyProperty ZAxisColorProperty;


		public Color CubeFaceMouseEnterColor
		{
			get { return ( Color )GetValue( CubeFaceMouseEnterColorProperty ); }
			set { SetValue( CubeFaceMouseEnterColorProperty, value ); }
		}
		public static readonly DependencyProperty CubeFaceMouseEnterColorProperty;


		public Color EdgeColor
		{
			get { return ( Color )GetValue( EdgeColorProperty ); }
			set { SetValue( EdgeColorProperty, value ); }
		}
		public static readonly DependencyProperty EdgeColorProperty;


		public Color EdgeMouseEnterColor
		{
			get { return ( Color )GetValue( EdgeMouseEnterColorProperty ); }
			set { SetValue( EdgeMouseEnterColorProperty, value ); }
		}
		public static readonly DependencyProperty EdgeMouseEnterColorProperty;


		public Color FontColor
		{
			get { return ( Color )GetValue( FontColorProperty ); }
			set { SetValue( FontColorProperty, value ); }
		}
		public static readonly DependencyProperty FontColorProperty;


		public double DiagonalLength
		{
			get { return ( double )GetValue( DiagonalLengthProperty ); }
			set { SetValue( DiagonalLengthProperty, value ); }
		}
		public static readonly DependencyProperty DiagonalLengthProperty;


		public Vector3D LookDirection
		{
			get { return ( Vector3D )GetValue( LookDirectionProperty ); }
			set { SetValue( LookDirectionProperty, value ); }
		}
		public static readonly DependencyProperty LookDirectionProperty;


		public Vector3D UpDirection
		{
			get { return ( Vector3D )GetValue( UpDirectionProperty ); }
			set { SetValue( UpDirectionProperty, value ); }
		}
		public static readonly DependencyProperty UpDirectionProperty;


		static ViewCube()
		{
			FrontTextProperty = DependencyProperty.Register(
				nameof( FrontText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Спереди", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Front ) ) );

			BackTextProperty = DependencyProperty.Register(
				nameof( BackText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Сзади", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Back ) ) );

			LeftTextProperty = DependencyProperty.Register(
				nameof( LeftText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Слева", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Left ) ) );

			RightTextProperty = DependencyProperty.Register(
				nameof( RightText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Справа", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Right ) ) );

			TopTextProperty = DependencyProperty.Register(
				nameof( TopText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Сверху", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Top ) ) );

			BottomTextProperty = DependencyProperty.Register(
				nameof( BottomText ),
				typeof( string ),
				typeof( ViewCube ),
				new PropertyMetadata( "Снизу", ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Bottom ) ) );

			XAxisColorProperty = DependencyProperty.Register(
				nameof( XAxisColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.Red, ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Top | CubeFaceType.Bottom ) ) );

			YAxisColorProperty = DependencyProperty.Register(
				nameof( YAxisColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.Blue, ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Left | CubeFaceType.Right ) ) );

			ZAxisColorProperty = DependencyProperty.Register(
				nameof( ZAxisColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.Green, ( d, e ) => FaceMaterialChangedCallback( d, e, CubeFaceType.Front | CubeFaceType.Back ) ) );

			CubeFaceMouseEnterColorProperty = DependencyProperty.Register(
				nameof( CubeFaceMouseEnterColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.LightSteelBlue ) );

			EdgeColorProperty = DependencyProperty.Register(
				nameof( EdgeColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.SteelBlue, EdgesMaterialChangedCallback ) );

			EdgeMouseEnterColorProperty = DependencyProperty.Register(
				nameof( EdgeMouseEnterColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.LightSteelBlue ) );

			FontColorProperty = DependencyProperty.Register(
				nameof( FontColor ),
				typeof( Color ),
				typeof( ViewCube ),
				new PropertyMetadata( Colors.White, FaceMaterialChangedCallback ) );

			DiagonalLengthProperty = DependencyProperty.Register(
				nameof( DiagonalLength ),
				typeof( double ),
				typeof( ViewCube ),
				new PropertyMetadata( 50.0, GeometryChangedCallback ) );

			LookDirectionProperty = DependencyProperty.Register(
				nameof( LookDirection ),
				typeof( Vector3D ),
				typeof( ViewCube ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ) ) );

			UpDirectionProperty = DependencyProperty.Register(
				nameof( UpDirection ),
				typeof( Vector3D ),
				typeof( ViewCube ),
				new PropertyMetadata( new Vector3D( 0, 1, 0 ) ) );
		}
		private static void EdgesMaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCube = ( ViewCube )d;

			viewCube.m_changedMaterialEdgesMask = CubeEdgeType.BackLeft
				| CubeEdgeType.BackRight
				| CubeEdgeType.BottomBack
				| CubeEdgeType.BottomFront
				| CubeEdgeType.BottomLeft
				| CubeEdgeType.BottomRight
				| CubeEdgeType.FrontLeft
				| CubeEdgeType.FrontRight
				| CubeEdgeType.TopBack
				| CubeEdgeType.TopFront
				| CubeEdgeType.TopLeft
				| CubeEdgeType.TopRight;

			viewCube.m_changedMaterialCornersMask = CubeCornerType.BottomBackLeft
				| CubeCornerType.BottomBackRight
				| CubeCornerType.BottomFrontLeft
				| CubeCornerType.BottomFrontRight
				| CubeCornerType.TopBackLeft
				| CubeCornerType.TopBackRight
				| CubeCornerType.TopFrontLeft
				| CubeCornerType.TopFrontRight;

			ChangedCallback( d, e );
		}
		private static void FaceMaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCube = ( ViewCube )d;
			viewCube.m_changedMaterialFacesMask = CubeFaceType.Front
				| CubeFaceType.Back
				| CubeFaceType.Left
				| CubeFaceType.Right
				| CubeFaceType.Top
				| CubeFaceType.Bottom;

			ChangedCallback( d, e );
		}
		private static void FaceMaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e, CubeFaceType changedFacesMask )
		{
			var viewCube = ( ViewCube )d;
			viewCube.m_changedMaterialFacesMask |= changedFacesMask;
			ChangedCallback( d, e );
		}
		private static void GeometryChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCube = ( ViewCube )d;

			viewCube.m_changedGeometryFacesMask = CubeFaceType.Front
				| CubeFaceType.Back
				| CubeFaceType.Left
				| CubeFaceType.Right
				| CubeFaceType.Top
				| CubeFaceType.Bottom;

			viewCube.m_changedGeometryEdgesMask = CubeEdgeType.BackLeft
				| CubeEdgeType.BackRight
				| CubeEdgeType.BottomBack
				| CubeEdgeType.BottomFront
				| CubeEdgeType.BottomLeft
				| CubeEdgeType.BottomRight
				| CubeEdgeType.FrontLeft
				| CubeEdgeType.FrontRight
				| CubeEdgeType.TopBack
				| CubeEdgeType.TopFront
				| CubeEdgeType.TopLeft
				| CubeEdgeType.TopRight;

			viewCube.m_changedGeometryCornersMask = CubeCornerType.BottomBackLeft
				| CubeCornerType.BottomBackRight
				| CubeCornerType.BottomFrontLeft
				| CubeCornerType.BottomFrontRight
				| CubeCornerType.TopBackLeft
				| CubeCornerType.TopBackRight
				| CubeCornerType.TopFrontLeft
				| CubeCornerType.TopFrontRight;

			ChangedCallback( d, e );
		}



		public ViewCube()
		{
			UpdateModel();
		}



		protected override void UpdateModel()
		{
			UpdateFaces();
			UpdateEdges();
			UpdateCorners();
		}

		#region Faces
		private void UpdateFaces()
		{
			if( ( m_changedGeometryFacesMask == 0 ) && ( m_changedMaterialFacesMask == 0 ) )
				return;

			foreach( CubeFaceType faceType in Enum.GetValues( typeof( CubeFaceType ) ) )
			{
				ModelUIElement3D face = null;
				GeometryModel3D faceModel = null;

				if( ( m_changedGeometryFacesMask & faceType ) != 0 )
				{
					face = GetFace( faceType );
					faceModel = GetElementModel( face );

					UpdateFaceGeometry( faceType, face, faceModel );
					m_changedGeometryFacesMask &= ~faceType;
				}

				if( ( m_changedMaterialFacesMask & faceType ) != 0 )
				{
					if( face == null )
					{
						face = GetFace( faceType );
						faceModel = GetElementModel( face );
					}

					UpdateFaceMaterial( faceType, face, faceModel );
					m_changedMaterialFacesMask &= ~faceType;
				}
			}
		}
		private void UpdateFaceGeometry( CubeFaceType type, ModelUIElement3D face, GeometryModel3D faceModel )
		{
			Vector3D normal = GetFaceNormal( type );
			Vector3D up = GetFaceUp( type );
			faceModel.Geometry = BuildFaceGeometry( normal, up );

			m_cubePartNormalVectors[ face ] = normal;
			m_cubePartUpVectors[ face ] = up;
		}
		private void UpdateFaceMaterial( CubeFaceType type, ModelUIElement3D face, GeometryModel3D faceModel )
		{
			var brush = GetFaceBrush( type );
			var faceText = GetFaceText( type );
			faceModel.Material = BuildFaceMaterial( brush, faceText );
		}
		private ModelUIElement3D GetFace( CubeFaceType type )
		{
			if( m_faces.ContainsKey( type ) )
			{
				var face = m_faces[ type ];
				if( face != null )
				{
					return face;
				}
			}
			return CreateFace( type );
		}
		private ModelUIElement3D CreateFace( CubeFaceType type )
		{
			var face = new ModelUIElement3D();
			face.MouseLeftButtonDown += OnMouseClick;
			face.MouseEnter += OnFaceMouseEnter;
			face.MouseLeave += ( s, e ) => OnFaceMouseLeave( s, e, type );

			m_faces[ type ] = face;
			Children.Add( face );

			return face;
		}
		private MeshGeometry3D BuildFaceGeometry( Vector3D normal, Vector3D up )
		{
			using( var builder = new MeshBuilder() )
			{
				builder.AddCubeSide( m_coordinateCenter, normal, up, Size, Size, Size );
				return builder.ToMesh();
			}
		}
		private Material BuildFaceMaterial( Brush backgroundBrush, string faceText )
		{
			var size = 100;

			var grid = new Grid
			{
				Width = size,
				Height = size,
				Background = backgroundBrush
			};

			var textBlock = new TextBlock
			{
				Text = faceText,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				FontSize = SIDE_TEXT_FONT_SIZE,
				Foreground = new SolidColorBrush( FontColor ),
				FontStretch = FontStretches.UltraExpanded,
				FontWeight = FontWeights.Bold,
			};

			grid.Children.Add( textBlock );
			grid.Arrange( new Rect( new Point( 0, 0 ), new Size( size, size ) ) );

			RenderTargetBitmap bmp = new RenderTargetBitmap( ( int )grid.Width, ( int )grid.Height, 96, 96, PixelFormats.Default );
			bmp.Render( grid );
			bmp.Freeze();

			var textBrush = new ImageBrush( bmp );

			Material material = MaterialHelper.CreateMaterial( textBrush );
			material.Freeze();

			return material;
		}
		private Vector3D GetFaceNormal( CubeFaceType type )
		{
			switch( type )
			{
				case CubeFaceType.Front:
					return BackToFrontDirection;

				case CubeFaceType.Back:
					return -BackToFrontDirection;

				case CubeFaceType.Left:
					return RightToLeftDirection;

				case CubeFaceType.Right:
					return -RightToLeftDirection;

				case CubeFaceType.Top:
					return BottomToTopDirection;

				case CubeFaceType.Bottom:
					return -BottomToTopDirection;

				default:
					return new Vector3D();
			}
		}
		private Vector3D GetFaceUp( CubeFaceType type )
		{
			switch( type )
			{
				case CubeFaceType.Front:
					return BottomToTopDirection;

				case CubeFaceType.Back:
					return BottomToTopDirection;

				case CubeFaceType.Left:
					return BottomToTopDirection;

				case CubeFaceType.Right:
					return BottomToTopDirection;

				case CubeFaceType.Top:
					return RightToLeftDirection;

				case CubeFaceType.Bottom:
					return -RightToLeftDirection;

				default:
					return new Vector3D();
			}
		}
		private Brush GetFaceBrush( CubeFaceType type )
		{
			switch( type )
			{
				case CubeFaceType.Front:
				case CubeFaceType.Back:
					return new SolidColorBrush( XAxisColor );

				case CubeFaceType.Left:
				case CubeFaceType.Right:
					return m_bottomToTopDirection.Z < 1
						? new SolidColorBrush( ZAxisColor )
						: new SolidColorBrush( YAxisColor );

				case CubeFaceType.Top:
				case CubeFaceType.Bottom:
					return m_bottomToTopDirection.Z < 1
						? new SolidColorBrush( YAxisColor )
						: new SolidColorBrush( ZAxisColor );

				default:
					return Brushes.White;
			}
		}
		private string GetFaceText( CubeFaceType type )
		{
			switch( type )
			{
				case CubeFaceType.Front:
					return FrontText;

				case CubeFaceType.Back:
					return BackText;

				case CubeFaceType.Left:
					return LeftText;

				case CubeFaceType.Right:
					return RightText;

				case CubeFaceType.Top:
					return TopText;

				case CubeFaceType.Bottom:
					return BottomText;

				default:
					return String.Empty;
			}
		}
		#endregion

		#region Edges
		private void UpdateEdges()
		{
			if( ( m_changedGeometryEdgesMask == 0 ) && ( m_changedMaterialEdgesMask == 0 ) )
				return;

			foreach( CubeEdgeType cubeEdgeType in Enum.GetValues( typeof( CubeEdgeType ) ) )
			{
				ModelUIElement3D edge = null;
				GeometryModel3D edgeModel = null;

				if( ( m_changedGeometryEdgesMask & cubeEdgeType ) != 0 )
				{
					edge = GetEdge( cubeEdgeType );
					edgeModel = GetElementModel( edge );

					UpdateEdgeGeometry( cubeEdgeType, edge, edgeModel );
					m_changedGeometryEdgesMask &= ~cubeEdgeType;
				}

				if( ( m_changedMaterialEdgesMask & cubeEdgeType ) != 0 )
				{
					if( edgeModel == null )
					{
						edge = GetEdge( cubeEdgeType );
						edgeModel = GetElementModel( edge );
					}

					UpdateEdgeMaterial( edgeModel );
					m_changedMaterialEdgesMask &= ~cubeEdgeType;
				}
			}
		}
		private void UpdateEdgeGeometry( CubeEdgeType type, ModelUIElement3D edge, GeometryModel3D edgeModel )
		{
			Point3D edgePoint = m_edgePoints[ type ];

			var halfSize = HalfSize;
			var quarterSize = QuarterSize;

			Point3D center = edgePoint.Multiply( halfSize );
			double x = GetEdgeX( type, halfSize, quarterSize );
			double y = GetEdgeY( type, halfSize, quarterSize );
			double z = GetEdgeZ( type, halfSize, quarterSize );

			edgeModel.Geometry = BuildEdgeGeometry( center, x, y, z );

			m_cubePartNormalVectors[ edge ] = edgePoint.ToVector3D();
			m_cubePartUpVectors[ edge ] = m_bottomToTopDirection;
		}
		private void UpdateEdgeMaterial( GeometryModel3D edgeModel )
		{
			edgeModel.Material = MaterialHelper.CreateMaterial( EdgeColor );
		}
		private double GetEdgeX( CubeEdgeType type, double halfSize, double quarterSize )
		{
			switch( type )
			{
				case CubeEdgeType.TopFront:
				case CubeEdgeType.TopBack:
				case CubeEdgeType.BottomFront:
				case CubeEdgeType.BottomBack:
					return halfSize * 1.5;

				default:
					return quarterSize;
			}
		}
		private double GetEdgeY( CubeEdgeType type, double halfSize, double quarterSize )
		{
			switch( type )
			{
				case CubeEdgeType.FrontRight:
				case CubeEdgeType.FrontLeft:
				case CubeEdgeType.BackRight:
				case CubeEdgeType.BackLeft:
					return halfSize * 1.5;

				default:
					return quarterSize;
			}
		}
		private double GetEdgeZ( CubeEdgeType type, double halfSize, double quarterSize )
		{
			switch( type )
			{
				case CubeEdgeType.TopRight:
				case CubeEdgeType.TopLeft:
				case CubeEdgeType.BottomRight:
				case CubeEdgeType.BottomLeft:
					return halfSize * 1.5;

				default:
					return quarterSize;
			}
		}
		private ModelUIElement3D GetEdge( CubeEdgeType type )
		{
			if( m_edges.ContainsKey( type ) )
			{
				var edge = m_edges[ type ];
				if( edge != null )
				{
					return edge;
				}
			}
			return CreateEdge( type );
		}
		private ModelUIElement3D CreateEdge( CubeEdgeType type )
		{
			var edge = new ModelUIElement3D();
			edge.MouseLeftButtonDown += OnMouseClick;
			edge.MouseEnter += OnEdgeMouseEnter;
			edge.MouseLeave += OnEdgeMouseLeave;

			m_edges[ type ] = edge;
			Children.Add( edge );

			return edge;
		}
		private MeshGeometry3D BuildEdgeGeometry( Point3D center, double x, double y, double z )
		{
			using( var builder = new MeshBuilder() )
			{
				builder.AddBox( center, x, y, z );
				return builder.ToMesh();
			}
		}
		#endregion

		#region Corners
		private void UpdateCorners()
		{
			if( ( m_changedGeometryCornersMask == 0 ) && ( m_changedMaterialCornersMask == 0 ) )
				return;

			foreach( CubeCornerType cubeCornerType in Enum.GetValues( typeof( CubeCornerType ) ) )
			{
				ModelUIElement3D corner = null;
				GeometryModel3D cornerModel = null;

				if( ( m_changedGeometryCornersMask & cubeCornerType ) != 0 )
				{
					corner = GetCorner( cubeCornerType );
					cornerModel = GetElementModel( corner );

					UpdateCornerGeometry( cubeCornerType, corner, cornerModel );
					m_changedGeometryCornersMask &= ~cubeCornerType;
				}

				if( ( m_changedMaterialCornersMask & cubeCornerType ) != 0 )
				{
					if( corner == null )
					{
						corner = GetCorner( cubeCornerType );
						cornerModel = GetElementModel( corner );
					}

					UpdateCornerMaterial( cubeCornerType, cornerModel );
					m_changedMaterialCornersMask &= ~cubeCornerType;
				}
			}
		}
		private void UpdateCornerGeometry( CubeCornerType cubeCornerType, ModelUIElement3D corner, GeometryModel3D cornerModel )
		{
			Point3D cornerPoint = m_cornerPoints[ cubeCornerType ];

			cornerModel.Geometry = BuildCornerGeometry( cornerPoint );

			m_cubePartNormalVectors[ corner ] = cornerPoint.ToVector3D();
			m_cubePartUpVectors[ corner ] = m_bottomToTopDirection;
		}
		private void UpdateCornerMaterial( CubeCornerType cubeCornerType, GeometryModel3D cornerModel )
		{
			cornerModel.Material = MaterialHelper.CreateMaterial( EdgeColor );
		}
		private ModelUIElement3D GetCorner( CubeCornerType type )
		{
			if( m_corners.ContainsKey( type ) )
			{
				var corner = m_corners[ type ];
				if( corner != null )
				{
					return corner;
				}
			}
			return CreateCorner( type );
		}
		private ModelUIElement3D CreateCorner( CubeCornerType type )
		{
			var corner = new ModelUIElement3D();
			corner.MouseLeftButtonDown += OnMouseClick;
			corner.MouseEnter += OnEdgeMouseEnter;
			corner.MouseLeave += OnEdgeMouseLeave;

			m_corners[ type ] = corner;
			Children.Add( corner );

			return corner;
		}
		private MeshGeometry3D BuildCornerGeometry( Point3D cornerPoint )
		{
			var halfSize = HalfSize;
			var quarterSize = QuarterSize;

			Point3D center = cornerPoint.Multiply( halfSize );

			using( var builder = new MeshBuilder() )
			{
				builder.AddBox( center, quarterSize, quarterSize, quarterSize );
				return builder.ToMesh();
			}
		}
		#endregion

		private GeometryModel3D GetElementModel( ModelUIElement3D uiElement )
		{
			var model = uiElement.Model as GeometryModel3D;
			if( model == null )
			{
				model = new GeometryModel3D();
				uiElement.Model = model;
			}
			return model;
		}

		#region Handlers
		private void OnMouseClick( object sender, MouseButtonEventArgs e )
		{
			var cubePart = ( ModelUIElement3D )sender;
			//var invert = e.ClickCount == 2;

			PickCubePart( cubePart/*, invert*/ );

			e.Handled = true;
		}
		private void PickCubePart( ModelUIElement3D cubePart/*, bool invert = false*/ )
		{
			if( cubePart == null )
				throw new ArgumentNullException( nameof( cubePart ) );

			Vector3D lookDirection = -m_cubePartNormalVectors[ cubePart ];
			lookDirection.Normalize();

			Vector3D upDirection = m_cubePartUpVectors[ cubePart ];
			upDirection.Normalize();

			//if( invert )
			//{
			//	lookDirection *= -1;
			//	if( upDirection != this.ModelUpDirection )
			//	{
			//		upDirection *= -1;
			//	}
			//}

			LookDirection = lookDirection;
			UpDirection = upDirection;
		}
		private void OnFaceMouseEnter( object sender, MouseEventArgs e )
		{
			ModelUIElement3D uiElement = sender as ModelUIElement3D;
			if( uiElement != null )
			{
				var geometryModel = uiElement.Model as GeometryModel3D;
				if( geometryModel != null )
				{
					geometryModel.Material = MaterialHelper.CreateMaterial( CubeFaceMouseEnterColor );
				}
			}
			e.Handled = true;
		}
		private void OnFaceMouseLeave( object sender, MouseEventArgs e, CubeFaceType cubeFace )
		{
			ModelUIElement3D uiElement = sender as ModelUIElement3D;
			if( uiElement != null )
			{
				var geometryModel = uiElement.Model as GeometryModel3D;
				if( geometryModel != null )
				{
					var brush = GetFaceBrush( cubeFace );
					var faceText = GetFaceText( cubeFace );

					geometryModel.Material = BuildFaceMaterial( brush, faceText );
				}
			}
			e.Handled = true;
		}
		private void OnEdgeMouseEnter( object sender, MouseEventArgs e )
		{
			ModelUIElement3D uiElement = sender as ModelUIElement3D;
			if( uiElement != null )
			{
				var geometryModel = uiElement.Model as GeometryModel3D;
				if( geometryModel != null )
				{
					geometryModel.Material = MaterialHelper.CreateMaterial( EdgeMouseEnterColor );
				}
			}
			e.Handled = true;
		}
		private void OnEdgeMouseLeave( object sender, MouseEventArgs e )
		{
			ModelUIElement3D uiElement = sender as ModelUIElement3D;
			if( uiElement != null )
			{
				var geometryModel = uiElement.Model as GeometryModel3D;
				if( geometryModel != null )
				{
					geometryModel.Material = MaterialHelper.CreateMaterial( EdgeColor );
				}
			}
			e.Handled = true;
		}
		#endregion

		public void PickCubeFace( CubeFaceType cubeFaceType )
		{
			if( m_faces.ContainsKey( cubeFaceType ) )
			{
				var cubeFace = m_faces[ cubeFaceType ];
				PickCubePart( cubeFace );
			}
		}
		public void Pick( Vector3D lookDirection, Vector3D upDirection )
		{
			LookDirection = lookDirection;
			UpDirection = upDirection;
		}
		public void Freeze()
		{
			foreach( var face in m_faces.Values )
			{
				var model = ( GeometryModel3D )face.Model;

				if( ( model.Geometry != null ) && ( model.Geometry.CanFreeze ) )
					model.Geometry.Freeze();

				if( ( model.Material != null ) && ( model.Material.CanFreeze ) )
					model.Material.Freeze();

				//if( face.Model.CanFreeze )
				//	face.Model.Freeze();
			}

			foreach( var edge in m_edges.Values )
			{
				var model = ( GeometryModel3D )edge.Model;

				if( ( model.Geometry != null ) && ( model.Geometry.CanFreeze ) )
					model.Geometry.Freeze();

				if( ( model.Material != null ) && ( model.Material.CanFreeze ) )
					model.Material.Freeze();

				//if( edge.Model.CanFreeze )
				//	edge.Model.Freeze();
			}

			foreach( var corner in m_corners.Values )
			{
				var model = ( GeometryModel3D )corner.Model;

				if( ( model.Geometry != null ) && ( model.Geometry.CanFreeze ) )
					model.Geometry.Freeze();

				if( ( model.Material != null ) && ( model.Material.CanFreeze ) )
					model.Material.Freeze();

				//if( corner.Model.CanFreeze )
				//	corner.Model.Freeze();
			}
		}




		[Flags]
		private enum CubeEdgeType : ushort
		{
			None = 0,
			TopFront = 1,
			TopBack = 1 << 1,
			TopRight = 1 << 2,
			TopLeft = 1 << 3,
			BottomFront = 1 << 4,
			BottomBack = 1 << 5,
			BottomRight = 1 << 6,
			BottomLeft = 1 << 7,
			FrontRight = 1 << 8,
			FrontLeft = 1 << 9,
			BackRight = 1 << 10,
			BackLeft = 1 << 11
		}

		[Flags]
		private enum CubeCornerType : byte
		{
			None = 0,
			TopFrontRight = 1,
			TopFrontLeft = 1 << 1,
			TopBackRight = 1 << 2,
			TopBackLeft = 1 << 3,
			BottomFrontRight = 1 << 4,
			BottomFrontLeft = 1 << 5,
			BottomBackRight = 1 << 6,
			BottomBackLeft = 1 << 7
		}
	}

	[Flags]
	public enum CubeFaceType : byte
	{
		None = 0,
		Front = 1,
		Back = 1 << 1,
		Left = 1 << 2,
		Right = 1 << 3,
		Top = 1 << 4,
		Bottom = 1 << 5
	}
}
