using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using WPF.Viewer3D.Tools;
using WPF.Viewer3D.Visuals;
using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace WPF.Viewer3D
{
	// TODO: Для компонентов модели не задавать BackMaterial - отрицательно влияет на производительность.
	// На данный момент BackMaterial задается, т.к. он отображается в сечениях.
	// Нужно сделать, чтобы срез сечения закрывался плоскостью.

	// TODO: Вынести настройки просмотровщика в Параметры отображения атрибута.

	// TODO: При быстром подряд нажатии на компоненты Куба просмотра,
	// если анимация для первого нажатия не завершилась, второе нажатие не отрботает.
	// Но, так как свойство зависимости в цепочке от Куба просмотра уже будет изменено,
	// повторно нажатие на элемент не отработает. Нужно принудительно вызывать PropertyChanged
	// для свойств зависимости в цепочке при нажатии на Куб просмотра.

	// TODO: Команда сброса настроек видимости отработает только при вращении модели с помощью
	// Куба просмотра, т.к. за вращение мышью отвечает другое свойство зависимости в CamerController.

	public partial class Scene : UserControl
	{
		private SceneModel m_viewModel;

		private CameraController m_cameraController;
		private SpotLightController m_spotLightController;

		private GridLines m_xGridLines;
		private GridLines m_yGridLines;
		private GridLines m_zGridLines;

		private CuttingManager m_cuttingManager;
		private CuttingData m_freeCuttingData;
		private CuttingData m_xCuttingData;
		private CuttingData m_yCuttingData;
		private CuttingData m_zCuttingData;

		private ExplodingManager m_explodingManager;

		private Rect3D m_modelBounds;
		private Point3D m_modelCenter;
		private double m_modelRadius;

		private Vector3D m_initLookDirection;
		private Vector3D m_initUpDirection;

		private bool m_isRightMouseButtonDown;
		private bool m_isLeftMouseButtonDown;
		private Point m_viewportCenterOnScreen;

		private List<Point3D> m_points = new List<Point3D>();
		private bool m_measureDistanceBetweenPoints = false;
		private bool m_measureDistanceBetweenPlanes = false;
		private bool IsInterfaceDisabled { get { return ( m_viewModel.Children == null ) || ( m_viewModel.Children.Count == 0 ); } }

		public Scene() : base()
		{
			InitializeComponent();

			// DataContext
			DataContext = m_viewModel = new SceneModel();

			// Viewport background
			var backgroundBrush = new LinearGradientBrush();
			backgroundBrush.StartPoint = new Point( 0.5, 0 );
			backgroundBrush.EndPoint = new Point( 0.5, 1 );
			backgroundBrush.GradientStops.Add( new GradientStop( Colors.LightSteelBlue, 0.0 ) );
			backgroundBrush.GradientStops.Add( new GradientStop( Colors.White, 0.75 ) );
			m_viewModel.BackgroundBrush = backgroundBrush;

			// Quality & Performance settings
			RenderOptions.SetEdgeMode( viewport, EdgeMode.Unspecified );
			RenderOptions.SetBitmapScalingMode( viewport, BitmapScalingMode.LowQuality );
			RenderOptions.SetCachingHint( viewport, CachingHint.Unspecified );
			RenderOptions.SetClearTypeHint( viewport, ClearTypeHint.Auto );
			viewport.IsHitTestVisible = false;

			InitToolBar();
			InitMouseHandlers();
			InitCamera();

			DemoOpen();
		}

		#region ToolBar

		private void InitToolBar()
		{
			#region File

			var fileItem = new MenuItem();
			fileItem.Header = "File";
			menu.Items.Add( fileItem );

			var fileOpenItem = new MenuItem();
			fileOpenItem.Header = "Open";
			fileOpenItem.Click += FileOpen;
			fileItem.Items.Add( fileOpenItem );

			var demoOpenItem = new MenuItem();
			demoOpenItem.Header = "Demo";
			demoOpenItem.Click += ( s, e ) => DemoOpen();
			fileItem.Items.Add( demoOpenItem );

			var scoobyOpenItem = new MenuItem();
			scoobyOpenItem.Header = "Scooby";
			scoobyOpenItem.Click += ( s, e ) => ScoobyOpen();
			fileItem.Items.Add( scoobyOpenItem );

			#endregion

			#region View

			var viewItem = new MenuItem();
			viewItem.Header = "View";
			menu.Items.Add( viewItem );

			var viewLeftItem = new MenuItem();
			viewLeftItem.Header = "Left";
			viewLeftItem.Click += OnViewLeft;
			viewItem.Items.Add( viewLeftItem );

			var viewFrontItem = new MenuItem();
			viewFrontItem.Header = "Front";
			viewFrontItem.Click += OnViewFront;
			viewItem.Items.Add( viewFrontItem );

			var viewRightItem = new MenuItem();
			viewRightItem.Header = "Right";
			viewRightItem.Click += OnViewRight;
			viewItem.Items.Add( viewRightItem );

			var viewBackItem = new MenuItem();
			viewBackItem.Header = "Back";
			viewBackItem.Click += OnViewBack;
			viewItem.Items.Add( viewBackItem );

			var viewTopItem = new MenuItem();
			viewTopItem.Header = "Top";
			viewTopItem.Click += OnViewTop;
			viewItem.Items.Add( viewTopItem );

			var viewBottomItem = new MenuItem();
			viewBottomItem.Header = "Bottom";
			viewBottomItem.Click += OnViewBottom;
			viewItem.Items.Add( viewBottomItem );

			var viewResetItem = new MenuItem();
			viewResetItem.Header = "Reset";
			viewResetItem.Click += OnViewReset;
			viewItem.Items.Add( viewResetItem );

			#endregion

			#region GridLines

			var gridItem = new MenuItem();
			gridItem.Header = "Grid";
			menu.Items.Add( gridItem );

			var xGridItem = new MenuItem();
			xGridItem.Header = "X";
			xGridItem.IsCheckable = true;
			xGridItem.Click += OnXGridLines;
			gridItem.Items.Add( xGridItem );

			var yGridItem = new MenuItem();
			yGridItem.Header = "Y";
			yGridItem.IsCheckable = true;
			yGridItem.Click += OnYGridLines;
			gridItem.Items.Add( yGridItem );

			var zGridItem = new MenuItem();
			zGridItem.Header = "Z";
			zGridItem.IsCheckable = true;
			zGridItem.Click += OnZGridLines;
			gridItem.Items.Add( zGridItem );

			var gridResetItem = new MenuItem();
			gridResetItem.Header = "Reset";
			gridResetItem.Click += OnGridLinesReset;
			gridItem.Items.Add( gridResetItem );

			#endregion

			#region Cut

			var cutItem = new MenuItem();
			cutItem.Header = "Cut";
			menu.Items.Add( cutItem );

			var freeCutItem = new MenuItem();
			freeCutItem.Header = "Free";
			cutItem.Items.Add( freeCutItem );

			var freeCutApplyItem = new MenuItem();
			freeCutApplyItem.Header = "Apply";
			freeCutApplyItem.IsCheckable = true;
			freeCutApplyItem.Click += OnFreeCutApply;
			freeCutItem.Items.Add( freeCutApplyItem );

			var freeCutUpdateItem = new MenuItem();
			freeCutUpdateItem.Header = "Update";
			freeCutUpdateItem.Click += OnFreeCutUpdate;
			freeCutItem.Items.Add( freeCutUpdateItem );

			var freeCutInvertItem = new MenuItem();
			freeCutInvertItem.Header = "Invert";
			freeCutInvertItem.IsCheckable = true;
			freeCutInvertItem.Click += OnFreeCutInvert;
			freeCutItem.Items.Add( freeCutInvertItem );

			var freeCutMoveItem = new ScrollBar();
			freeCutMoveItem.Width = 200;
			freeCutMoveItem.Orientation = Orientation.Horizontal;
			freeCutMoveItem.Minimum = -1;
			freeCutMoveItem.Value = 0;
			freeCutMoveItem.Maximum = 1;
			freeCutMoveItem.ValueChanged += OnFreeCutMove;
			freeCutItem.Items.Add( freeCutMoveItem );

			var xCutItem = new MenuItem();
			xCutItem.Header = "X";
			cutItem.Items.Add( xCutItem );

			var xCutApplyItem = new MenuItem();
			xCutApplyItem.Header = "Apply";
			xCutApplyItem.IsCheckable = true;
			xCutApplyItem.Click += OnXCutApply;
			xCutItem.Items.Add( xCutApplyItem );

			var xCutInvertItem = new MenuItem();
			xCutInvertItem.Header = "Invert";
			xCutInvertItem.Click += OnXCutInvert;
			xCutItem.Items.Add( xCutInvertItem );

			var xCutMoveItem = new ScrollBar();
			xCutMoveItem.Width = 200;
			xCutMoveItem.Orientation = Orientation.Horizontal;
			xCutMoveItem.Minimum = -1;
			xCutMoveItem.Value = 0;
			xCutMoveItem.Maximum = 1;
			xCutMoveItem.ValueChanged += OnXCutMove;
			xCutItem.Items.Add( xCutMoveItem );

			var yCutItem = new MenuItem();
			yCutItem.Header = "Y";
			cutItem.Items.Add( yCutItem );

			var yCutApplyItem = new MenuItem();
			yCutApplyItem.Header = "Apply";
			yCutApplyItem.IsCheckable = true;
			yCutApplyItem.Click += OnYCutApply;
			yCutItem.Items.Add( yCutApplyItem );

			var yCutInvertItem = new MenuItem();
			yCutInvertItem.Header = "Invert";
			yCutInvertItem.Click += OnYCutInvert;
			yCutItem.Items.Add( yCutInvertItem );

			var yCutMoveItem = new ScrollBar();
			yCutMoveItem.Width = 200;
			yCutMoveItem.Orientation = Orientation.Horizontal;
			yCutMoveItem.Minimum = -1;
			yCutMoveItem.Value = 0;
			yCutMoveItem.Maximum = 1;
			yCutMoveItem.ValueChanged += OnYCutMove;
			yCutItem.Items.Add( yCutMoveItem );

			var zCutItem = new MenuItem();
			zCutItem.Header = "Z";
			cutItem.Items.Add( zCutItem );

			var zCutApplyItem = new MenuItem();
			zCutApplyItem.Header = "Apply";
			zCutApplyItem.IsCheckable = true;
			zCutApplyItem.Click += OnZCutApply;
			zCutItem.Items.Add( zCutApplyItem );

			var zCutInvertItem = new MenuItem();
			zCutInvertItem.Header = "Invert";
			zCutInvertItem.Click += OnZCutInvert;
			zCutItem.Items.Add( zCutInvertItem );

			var zCutMoveItem = new ScrollBar();
			zCutMoveItem.Width = 200;
			zCutMoveItem.Orientation = Orientation.Horizontal;
			zCutMoveItem.Minimum = -1;
			zCutMoveItem.Value = 0;
			zCutMoveItem.Maximum = 1;
			zCutMoveItem.ValueChanged += OnZCutMove;
			zCutItem.Items.Add( zCutMoveItem );

			var cutResetItem = new MenuItem();
			cutResetItem.Header = "Reset";
			cutResetItem.Click += OnCutReset;
			cutItem.Items.Add( cutResetItem );

			#endregion

			#region Explode

			var explodeItem = new MenuItem();
			explodeItem.Header = "Explode";
			menu.Items.Add( explodeItem );

			var explodeChangeItem = new ScrollBar();
			explodeChangeItem.Width = 200;
			explodeChangeItem.Orientation = Orientation.Horizontal;
			explodeChangeItem.Minimum = 0;
			explodeChangeItem.Value = 0;
			explodeChangeItem.Maximum = 100;
			explodeChangeItem.ValueChanged += OnExplodeChange;
			explodeItem.Items.Add( explodeChangeItem );

			#endregion

			#region Distance

			var distanceBetweenPoints = new MenuItem();
			distanceBetweenPoints.Header = "Distance (Points)";
			distanceBetweenPoints.IsCheckable = true;
			distanceBetweenPoints.Click += OnDistanceBetweenPoints;
			menu.Items.Add( distanceBetweenPoints );

			var distanceBetweenPlanes = new MenuItem();
			distanceBetweenPlanes.Header = "Distance (Planes)";
			distanceBetweenPlanes.IsCheckable = true;
			distanceBetweenPlanes.Click += OnDistanceBetweenPlanes;
			menu.Items.Add( distanceBetweenPlanes );

			#endregion
		}

		#region File

		private void FileOpen( object sender, RoutedEventArgs e )
		{
			var openFileDialog = new OpenFileDialog();
			openFileDialog.DefaultExt = ".xaml";
			openFileDialog.Filter = "Xaml files (.xaml)|*.xaml";
			if( openFileDialog.ShowDialog() == true )
			{
				var fileName = openFileDialog.FileName;
				var xaml = File.ReadAllText( fileName );
				LoadXaml( xaml );
			}
		}

		private void DemoOpen()
		{
			var xaml = File.ReadAllText( @"Models/Demo.xaml" );
			LoadXaml( xaml );
		}

		private void ScoobyOpen()
		{
			var xaml = File.ReadAllText( @"Models/Scooby.xaml" );
			LoadXaml( xaml );
		}

		#endregion

		#region View

		private void OnViewLeft( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Left );
		}

		private void OnViewFront( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Front );
		}

		private void OnViewRight( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Right );
		}

		private void OnViewBack( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Back );
		}

		private void OnViewTop( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Top );
		}

		private void OnViewBottom( object sender, RoutedEventArgs e )
		{
			viewCubeControl.PickCubeFace( CubeFaceType.Bottom );
		}

		private void OnViewReset( object sender, RoutedEventArgs e )
		{
			ExplodeReset();
			viewCubeControl.Pick( m_initLookDirection, m_initUpDirection );
		}

		#endregion

		#region GridLines

		private void OnXGridLines( object sender, RoutedEventArgs e )
		{
			if( m_xGridLines == null )
			{
				m_xGridLines = new GridLines();
				using( new SuspendRender( m_xGridLines ) )
				{
					m_xGridLines.Normal = new Vector3D( 0, 0, 1 );
					m_xGridLines.Material = m_xGridLines.BackMaterial = MaterialHelper.CreateMaterial( Colors.Red );
					m_xGridLines.Thickness = m_modelRadius * 0.001;
					m_xGridLines.SmallCellSize = m_modelRadius * 0.1;
					m_xGridLines.LargeCellSize = m_modelRadius;
					m_xGridLines.Height = m_modelRadius * 3;
					m_xGridLines.Width = m_modelRadius * 3;
				}
				viewport.Children.Add( m_xGridLines );
			}
			else
			{
				viewport.Children.Remove( m_xGridLines );
				m_xGridLines = null;
			}
		}

		private void OnYGridLines( object sender, RoutedEventArgs e )
		{
			if( m_yGridLines == null )
			{
				m_yGridLines = new GridLines();
				using( new SuspendRender( m_yGridLines ) )
				{
					m_yGridLines.Normal = new Vector3D( 0, 1, 0 );
					m_yGridLines.Material = m_yGridLines.BackMaterial = MaterialHelper.CreateMaterial( Colors.Blue );
					m_yGridLines.Thickness = m_modelRadius * 0.001;
					m_yGridLines.SmallCellSize = m_modelRadius * 0.1;
					m_yGridLines.LargeCellSize = m_modelRadius;
					m_yGridLines.Height = m_modelRadius * 3;
					m_yGridLines.Width = m_modelRadius * 3;
				}
				viewport.Children.Add( m_yGridLines );
			}
			else
			{
				viewport.Children.Remove( m_yGridLines );
				m_yGridLines = null;
			}
		}

		private void OnZGridLines( object sender, RoutedEventArgs e )
		{
			if( m_zGridLines == null )
			{
				m_zGridLines = new GridLines();
				using( new SuspendRender( m_zGridLines ) )
				{
					m_zGridLines.Normal = new Vector3D( 1, 0, 0 );
					m_zGridLines.Material = m_zGridLines.BackMaterial = MaterialHelper.CreateMaterial( Colors.Green );
					m_zGridLines.Thickness = m_modelRadius * 0.001;
					m_zGridLines.SmallCellSize = m_modelRadius * 0.1;
					m_zGridLines.LargeCellSize = m_modelRadius;
					m_zGridLines.Height = m_modelRadius * 3;
					m_zGridLines.Width = m_modelRadius * 3;
				}
				viewport.Children.Add( m_zGridLines );
			}
			else
			{
				viewport.Children.Remove( m_zGridLines );
				m_zGridLines = null;
			}
		}

		private void OnGridLinesReset( object sender, RoutedEventArgs e )
		{
			GridLinesReset();
		}

		private void GridLinesReset()
		{
			if( m_xGridLines != null )
			{
				viewport.Children.Remove( m_xGridLines );
				m_xGridLines = null;
			}

			if( m_yGridLines != null )
			{
				viewport.Children.Remove( m_yGridLines );
				m_yGridLines = null;
			}

			if( m_zGridLines != null )
			{
				viewport.Children.Remove( m_zGridLines );
				m_zGridLines = null;
			}
		}

		#endregion

		#region Cut

		private void OnFreeCutApply( object sender, RoutedEventArgs e )
		{
			if( m_freeCuttingData.IsApplied )
			{
				m_cuttingManager.Remove( m_freeCuttingData );
				m_freeCuttingData.IsApplied = false;
			}
			else
			{
				m_freeCuttingData = new CuttingData( m_modelCenter, m_cameraController.LookDirection );
				m_cuttingManager.Add( m_freeCuttingData );
				m_freeCuttingData.IsApplied = true;
			}
		}

		private void OnFreeCutUpdate( object sender, RoutedEventArgs e )
		{
			m_cuttingManager.Remove( m_freeCuttingData, false );
			m_freeCuttingData.Normal = m_cameraController.LookDirection;
			m_cuttingManager.Add( m_freeCuttingData );
		}

		private void OnFreeCutInvert( object sender, RoutedEventArgs e )
		{
			m_cuttingManager.Remove( m_freeCuttingData, false );
			m_freeCuttingData.IsComplement = !m_freeCuttingData.IsComplement;
			m_cuttingManager.Add( m_freeCuttingData );
		}

		private void OnFreeCutMove( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			var scrollBar = sender as ScrollBar;
			if( scrollBar == null )
				return;

			m_cuttingManager.Remove( m_freeCuttingData, false );
			m_freeCuttingData.MovedPoint = CalculateMovePoint( m_freeCuttingData, scrollBar.Value );
			m_cuttingManager.Add( m_freeCuttingData );
		}

		private Point3D CalculateMovePoint( CuttingData cuttingData, double delta )
		{
			var temp = cuttingData.Normal;
			temp.Normalize();
			temp *= m_modelRadius * delta;

			return cuttingData.Point + temp;
		}

		private void OnXCutApply( object sender, RoutedEventArgs e )
		{
			if( m_xCuttingData.IsApplied )
			{
				m_cuttingManager.Remove( m_xCuttingData );
				m_xCuttingData.IsApplied = false;
			}
			else
			{
				ExplodeReset();

				m_cuttingManager.Add( m_xCuttingData );
				m_xCuttingData.IsApplied = true;
			}
		}

		private void OnXCutInvert( object sender, RoutedEventArgs e )
		{
			m_cuttingManager.Remove( m_xCuttingData, false );
			m_xCuttingData.IsComplement = !m_xCuttingData.IsComplement;
			m_cuttingManager.Add( m_xCuttingData );
		}

		private void OnXCutMove( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			var scrollBar = ( ScrollBar )sender;

			m_cuttingManager.Remove( m_xCuttingData, false );
			m_xCuttingData.MovedPoint = CalculateMovePoint( m_xCuttingData, scrollBar.Value );
			m_cuttingManager.Add( m_xCuttingData );
		}

		private void OnYCutApply( object sender, RoutedEventArgs e )
		{
			if( m_yCuttingData.IsApplied )
			{
				m_cuttingManager.Remove( m_yCuttingData );
				m_yCuttingData.IsApplied = false;
			}
			else
			{
				ExplodeReset();

				m_cuttingManager.Add( m_yCuttingData );
				m_yCuttingData.IsApplied = true;
			}
		}

		private void OnYCutInvert( object sender, RoutedEventArgs e )
		{
			m_cuttingManager.Remove( m_yCuttingData, false );
			m_yCuttingData.IsComplement = !m_yCuttingData.IsComplement;
			m_cuttingManager.Add( m_yCuttingData );
		}

		private void OnYCutMove( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			var scrollBar = ( ScrollBar )sender;

			m_cuttingManager.Remove( m_yCuttingData, false );
			m_yCuttingData.MovedPoint = CalculateMovePoint( m_yCuttingData, scrollBar.Value );
			m_cuttingManager.Add( m_yCuttingData );
		}

		private void OnZCutApply( object sender, RoutedEventArgs e )
		{
			if( m_zCuttingData.IsApplied )
			{
				m_cuttingManager.Remove( m_zCuttingData );
				m_zCuttingData.IsApplied = false;
			}
			else
			{
				ExplodeReset();

				m_cuttingManager.Add( m_zCuttingData );
				m_zCuttingData.IsApplied = true;
			}
		}

		private void OnZCutInvert( object sender, RoutedEventArgs e )
		{
			m_cuttingManager.Remove( m_zCuttingData, false );
			m_zCuttingData.IsComplement = !m_zCuttingData.IsComplement;
			m_cuttingManager.Add( m_zCuttingData );
		}

		private void OnZCutMove( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			var scrollBar = ( ScrollBar )sender;

			m_cuttingManager.Remove( m_zCuttingData, false );
			m_zCuttingData.MovedPoint = CalculateMovePoint( m_zCuttingData, scrollBar.Value );
			m_cuttingManager.Add( m_zCuttingData );
		}

		private void OnCutReset( object sender, RoutedEventArgs e )
		{
			CutReset();
		}

		private void CutReset()
		{
			m_cuttingManager.RemoveAll();
			m_freeCuttingData.IsApplied = false;
			m_xCuttingData.IsApplied = false;
			m_yCuttingData.IsApplied = false;
			m_zCuttingData.IsApplied = false;
		}

		#endregion

		#region Explode

		private void OnExplodeChange( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			var scrollBar = ( ScrollBar )sender;

			GridLinesReset();
			CutReset();

			var delta = m_modelRadius * scrollBar.Value;
			m_explodingManager.Apply( delta );
		}

		private void ExplodeReset()
		{
			m_explodingManager.Reset();
		}

		#endregion

		#region Distance

		private void OnDistanceBetweenPoints( object sender, RoutedEventArgs e )
		{
			var menuItem = sender as MenuItem;
			if( menuItem == null )
				return;

			m_measureDistanceBetweenPoints = menuItem.IsChecked;
		}

		private void OnDistanceBetweenPlanes( object sender, RoutedEventArgs e )
		{
			var menuItem = sender as MenuItem;
			if( menuItem == null )
				return;

			m_measureDistanceBetweenPlanes = menuItem.IsChecked;
		}

		#endregion

		#endregion

		#region MouseHandlers

		private void InitMouseHandlers()
		{
			// Нельзя просто повесить обработчики на область просмотра (viewport), т.к. действия мыши тогда будут перехватываться только при наведении мыши на модель.
			// Холст (canvas) же растягивается на всю площадь родительской сетки (grid), позволяет реагировать на мышь в любой ее точке.
			canvas.MouseDown += OnMouseDown;
			canvas.MouseUp += OnMouseUp;
			canvas.MouseMove += OnMouseMove;
			canvas.MouseWheel += OnMouseWheel;
		}

		private void OnMouseDown( object sender, MouseButtonEventArgs e )
		{
			if( IsInterfaceDisabled )
				return;

			if( e.RightButton == MouseButtonState.Pressed )
			{
				m_isRightMouseButtonDown = true;

				MoveMouseToViewportCenter( true );

				this.Cursor = Cursors.None;
			}
			else if( e.LeftButton == MouseButtonState.Pressed )
			{
				if( m_measureDistanceBetweenPoints )
				{
					var positionOnScreen = e.GetPosition( viewport );

					Point3D p;
					if( viewport.TryHitPoint( positionOnScreen, out p ) )
					{
						var sphere = new Sphere();
						using( new SuspendRender( sphere ) )
						{
							sphere.Center = p;
							sphere.Radius = 0.001;
							sphere.VerticalDivisionNumber = 18;
							sphere.HorizontalDivisionNumber = 18;
							sphere.Material = MaterialHelper.CreateMaterial( Colors.Red );
						}
						sphere.Content.Freeze();
						viewport.Children.Add( sphere );

						m_points.Add( p );
						if( m_points.Count == 2 )
						{
							var pipe = new Pipe( m_points[ 0 ], m_points[ 1 ] );
							viewport.Children.Add( pipe );

							SetTransparentMaterials( viewport.Children );

							var distance = m_points[ 1 ] - m_points[ 0 ];
							MessageBox.Show( distance.Length.ToString() );
							m_points.Clear();
						}
					}
				}

				if( m_measureDistanceBetweenPlanes )
				{
					var positionOnScreen = e.GetPosition( viewport );

					GeometryModel3D model;
					if( viewport.TryHitModel( positionOnScreen, out model ) )
					{
						model.Material = MaterialHelper.CreateMaterial( Colors.Gold );
					}
				}
			}
		}

		private void OnMouseUp( object sender, MouseButtonEventArgs e )
		{
			if( IsInterfaceDisabled )
				return;

			if( m_isRightMouseButtonDown && ( e.RightButton != MouseButtonState.Pressed ) )
			{
				m_isRightMouseButtonDown = false;

				this.Cursor = Cursors.Arrow;
			}
			else if( m_isLeftMouseButtonDown && e.LeftButton != MouseButtonState.Pressed )
			{
				m_isLeftMouseButtonDown = false;

				this.Cursor = Cursors.Arrow;
			}
		}

		private void OnMouseMove( object sender, MouseEventArgs e )
		{
			if( IsInterfaceDisabled )
				return;

			if( m_isRightMouseButtonDown )
			{
				Point prevMousePosition = new Point( viewport.ActualWidth / 2, viewport.ActualHeight / 2 );
				Point curMousePosition = Mouse.GetPosition( viewport );
				Vector delta = curMousePosition - prevMousePosition;

				m_cameraController.Rotate( delta );

				MoveMouseToViewportCenter();
			}
			else if( m_isLeftMouseButtonDown )
			{

			}
		}

		private void OnMouseWheel( object sender, MouseWheelEventArgs e )
		{
			if( IsInterfaceDisabled )
				return;

			m_cameraController.Zoom( e.Delta );
		}

		private void MoveMouseToViewportCenter( bool updateViewportCenter = false )
		{
			if( updateViewportCenter )
			{
				var viewportCenterCoordinates = new Point( viewport.ActualWidth / 2, viewport.ActualHeight / 2 );
				m_viewportCenterOnScreen = viewport.PointToScreen( viewportCenterCoordinates );
			}

			MouseHelper.SetPosition( m_viewportCenterOnScreen );
		}

		#endregion

		#region Camera

		private void InitCamera()
		{
			m_cameraController = new CameraController( viewport );
			BindViewCubeToCamera();
			BindCoordinateSystemToCamera();
		}

		private void BindViewCubeToCamera()
		{
			BindingOperations.SetBinding( m_cameraController, CameraController.AnimateToLookDirectionProperty, new Binding
			{
				Source = viewCubeControl.CameraController,
				Path = new PropertyPath( nameof( CameraController.AnimateToLookDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( m_cameraController, CameraController.AnimateToUpDirectionProperty, new Binding
			{
				Source = viewCubeControl.CameraController,
				Path = new PropertyPath( nameof( CameraController.AnimateToUpDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( viewCubeControl.CameraController, CameraController.MoveToUpDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.MoveToUpDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( viewCubeControl.CameraController, CameraController.MoveToLookDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.MoveToLookDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );
		}

		private void BindCoordinateSystemToCamera()
		{
			BindingOperations.SetBinding( coordinateSystemControl.CameraController, CameraController.MoveToUpDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.MoveToUpDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( coordinateSystemControl.CameraController, CameraController.MoveToLookDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.MoveToLookDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( coordinateSystemControl.CameraController, CameraController.AnimateToUpDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.AnimateToUpDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( coordinateSystemControl.CameraController, CameraController.AnimateToLookDirectionProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.AnimateToLookDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );
		}

		#endregion

		public void LoadXaml( string data )
		{
			try
			{
				Viewport3D sourceViewport = null;
				if( !TryGetViewport( data, out sourceViewport ) )
					return;

				m_modelBounds = sourceViewport.GetModelBounds();
				m_modelCenter = sourceViewport.GetModelCenter( m_modelBounds );
				m_modelRadius = sourceViewport.GetModelRadius( m_modelBounds );

				#region Camera position update
				var sourceCamera = ( ProjectionCamera )sourceViewport.Camera;
				var sourceLookDirection = sourceCamera.LookDirection;
				sourceLookDirection.Normalize();
				sourceLookDirection *= m_modelRadius * 5;

				m_cameraController.RotateAround = m_modelCenter;
				m_cameraController.MinDistance = m_modelRadius;
				m_cameraController.LookDirection = m_initLookDirection = sourceLookDirection;
				m_cameraController.UpDirection = m_initUpDirection = sourceCamera.UpDirection;
				#endregion

				#region Viewport composition update
				m_viewModel.Children.Clear();

				var children = new Visual3D[ sourceViewport.Children.Count ];
				sourceViewport.Children.CopyTo( children, 0 );

				bool isLightsReplaced = false;
				foreach( var child in children )
				{
					var modelVisual = child as ModelVisual3D;
					if( modelVisual != null )
					{
						sourceViewport.Children.Remove( modelVisual );

						PrepareModelVisual( modelVisual, ref isLightsReplaced );

						m_viewModel.Children.Add( modelVisual );
					}
				}
				#endregion

				#region Cutting parameters update
				m_cuttingManager = new CuttingManager( viewport.Children );
				m_xCuttingData = new CuttingData( m_modelCenter, new Vector3D( 0, 0, 1 ) );
				m_yCuttingData = new CuttingData( m_modelCenter, new Vector3D( 0, -1, 0 ) );
				m_zCuttingData = new CuttingData( m_modelCenter, new Vector3D( 1, 0, 0 ) );
				#endregion

				#region Explode parameters update
				m_explodingManager = new ExplodingManager( viewport.Children, m_modelCenter );
				#endregion
			}
			catch( Exception ex )
			{
			}
		}

		private bool TryGetViewport( string data, out Viewport3D viewport )
		{
			try
			{
				viewport = ( Viewport3D )XamlReader.Parse( data );
				return true;
			}
			catch( Exception ex )
			{
				viewport = null;
				MessageBox.Show ( @"Не удалось извлечь данные модели из файла. Убедитесь, что атрибут содержит xaml-файл с моделью." );
				return false;
			}
		}

		private void PrepareModelVisual( ModelVisual3D mv, ref bool isLightsReplaced )
		{
			if( mv.Content != null )
			{
				var mg = mv.Content as Model3DGroup;
				if( mg != null )
				{
					if( mg.Children != null && mg.Children.Count != 0 )
					{
						var lights = new List<Light>();
						foreach( var mgChild in mg.Children )
						{
							var light = mgChild as Light;
							if( light != null )
							{
								lights.Add( light );
							}
						}

						if( lights.Count != 0 )
						{
							foreach( var light in lights )
							{
								mg.Children.Remove( light );
							}

							if( !isLightsReplaced )
							{
								var ambientLight = new AmbientLight();
								ambientLight.Color = Colors.DarkSlateGray;
								mg.Children.Add( ambientLight );

								var spotLight = new SpotLight();
								spotLight.Color = Colors.DarkGray;
								mg.Children.Add( spotLight );

								m_spotLightController = new SpotLightController( spotLight );
								m_spotLightController.RotateAround = m_modelCenter;

								var currentLookDirection = m_cameraController.LookDirection;
								currentLookDirection.Normalize();
								currentLookDirection *= m_modelRadius * 5;

								m_spotLightController.LookDirection = currentLookDirection;
								//m_spotLightController.Position = m_modelCenter - m_cameraController.LookDirection;

								BindingOperations.SetBinding( m_spotLightController, SpotLightController.AnimateToLookDirectionProperty, new Binding()
								{
									Source = m_cameraController,
									Path = new PropertyPath( nameof( CameraController.AnimateToLookDirection ) ),
									UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
									Mode = BindingMode.OneWay
								} );

								BindingOperations.SetBinding( m_spotLightController, SpotLightController.MoveToLookDirectionProperty, new Binding()
								{
									Source = m_cameraController,
									Path = new PropertyPath( nameof( CameraController.MoveToLookDirection ) ),
									UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
									Mode = BindingMode.OneWay
								} );
								//m_spotLightController.Subscribe( m_cameraController );

								isLightsReplaced = true;
							}
						}
					}
				}
			}

			if( mv.Children != null && mv.Children.Count != 0 )
			{
				foreach( var visual in mv.Children )
				{
					var modelVisual = visual as ModelVisual3D;
					if( modelVisual != null )
					{
						var modelGroup = modelVisual.Content as Model3DGroup;
						if( modelGroup != null )
						{
							if( modelGroup.Children != null && modelGroup.Children.Count != 0 )
							{
								foreach( var model in modelGroup.Children )
								{
									var geometryModel = model as GeometryModel3D;
									if( geometryModel != null && geometryModel.BackMaterial == null )
									{
										geometryModel.BackMaterial = geometryModel.Material;

										RenderOptions.SetEdgeMode( geometryModel, EdgeMode.Unspecified );
										RenderOptions.SetBitmapScalingMode( geometryModel, BitmapScalingMode.LowQuality );
										RenderOptions.SetCachingHint( geometryModel, CachingHint.Unspecified );
										RenderOptions.SetClearTypeHint( geometryModel, ClearTypeHint.Auto );
									}
								}
							}
						}
						PrepareModelVisual( modelVisual, ref isLightsReplaced );
					}
				}
			}
		}

		private void SetTransparentMaterials( Visual3DCollection children )
		{
			if( children == null || children.Count == 0 )
				return;

			foreach( var child in children )
			{
				var modelVisual = child as ModelVisual3D;
				if( modelVisual != null )
				{
					var modelGroup = modelVisual.Content as Model3DGroup;
					if( modelGroup != null )
					{
						foreach( var mgChild in modelGroup.Children )
						{
							var model = mgChild as GeometryModel3D;
							if( model != null )
							{
								model.Material = MaterialHelper.CreateMaterial( Colors.White, 0.5 );//  model.Material;
								model.BackMaterial = MaterialHelper.CreateMaterial( Colors.White, 0.5 );
							}
						}
					}
					SetTransparentMaterials( modelVisual.Children );
				}
			}
		}

		public Ray3D GetRay( Viewport3D viewport, Point position )
		{
			Point3D point1, point2;
			bool ok = Point2DtoPoint3D( viewport, position, out point1, out point2 );
			if( !ok )
			{
				return null;
			}

			return new Ray3D { Origin = point1, Direction = point2 - point1 };
		}

		public bool Point2DtoPoint3D( Viewport3D viewport, Point pointIn, out Point3D pointNear, out Point3D pointFar )
		{
			pointNear = new Point3D();
			pointFar = new Point3D();

			var pointIn3D = new Point3D( pointIn.X, pointIn.Y, 0 );
			var matrixViewport = viewport.GetViewportTransform();
			var matrixCamera = viewport.GetCameraTransform();

			if( !matrixViewport.HasInverse )
			{
				return false;
			}

			if( !matrixCamera.HasInverse )
			{
				return false;
			}

			matrixViewport.Invert();
			matrixCamera.Invert();

			var pointNormalized = matrixViewport.Transform( pointIn3D );
			pointNormalized.Z = 0.01;
			pointNear = matrixCamera.Transform( pointNormalized );
			pointNormalized.Z = 0.99;
			pointFar = matrixCamera.Transform( pointNormalized );

			return true;
		}
	}

	internal class SceneModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged( string propName )
		{
			if( PropertyChanged != null )
				PropertyChanged( this, new PropertyChangedEventArgs( propName ) );
		}

		public SceneModel()
		{
			Children = new ObservableCollection<ModelVisual3D>();
		}

		private PerspectiveCamera m_camera;
		public PerspectiveCamera Camera
		{
			get { return m_camera; }
			set
			{
				if( m_camera != value )
				{
					m_camera = value;
					OnPropertyChanged( "Camera" );
				}
			}
		}


		//private Model3DGroup m_lights;
		//public Model3DGroup Lights
		//{
		//	get { return m_lights; }
		//	set
		//	{
		//		if( m_lights != value )
		//		{
		//			m_lights = value;
		//			OnPropertyChanged( "Lights" );
		//		}
		//	}
		//}


		//private Model3DGroup m_model;
		//public Model3DGroup Model
		//{
		//	get { return m_model; }
		//	set
		//	{
		//		if( m_model != value )
		//		{
		//			m_model = value;
		//			OnPropertyChanged( "Model" );
		//		}
		//	}
		//}

		private Material m_XAxisMaterial;
		public Material XAxisMaterial
		{
			get { return m_XAxisMaterial; }
			set
			{
				if( m_XAxisMaterial != value )
				{
					m_XAxisMaterial = value;
					OnPropertyChanged( "XAxisMaterial" );
				}
			}
		}

		private Material m_YAxisMaterial;
		public Material YAxisMaterial
		{
			get { return m_YAxisMaterial; }
			set
			{
				if( m_YAxisMaterial != value )
				{
					m_YAxisMaterial = value;
					OnPropertyChanged( "YAxisMaterial" );
				}
			}
		}

		private Material m_ZAxisMaterial;
		public Material ZAxisMaterial
		{
			get { return m_ZAxisMaterial; }
			set
			{
				if( m_ZAxisMaterial != value )
				{
					m_ZAxisMaterial = value;
					OnPropertyChanged( "ZAxisMaterial" );
				}
			}
		}


		private Brush m_backgroundBrush;
		/// <summary>
		/// Кисть для заполнения фона области просмотра.
		/// </summary>
		public Brush BackgroundBrush
		{
			get { return m_backgroundBrush; }
			set
			{
				if( m_backgroundBrush != value )
				{
					m_backgroundBrush = value;
					OnPropertyChanged( "BackgroundBrush" );
				}
			}
		}

		/// <summary>
		/// Отображаемые модели.
		/// </summary>
		public ObservableCollection<ModelVisual3D> Children { get; set; }
	}
}
