using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WPF.Viewer3D.Visuals;

namespace WPF.Viewer3D
{
	public class CoordinateSystemControl : UserControl
	{
		private Viewport3D m_viewport;
		private CoordinateSystem m_coordinateSystem;

		private CameraController m_cameraController;
		public CameraController CameraController { get { return m_cameraController; } }


		public double Size
		{
			get { return ( double )GetValue( SizeProperty ); }
			set { SetValue( SizeProperty, value ); }
		}
		public static readonly DependencyProperty SizeProperty;


		static CoordinateSystemControl()
		{
			SizeProperty = DependencyProperty.Register(
				nameof( Size ),
				typeof( double ),
				typeof( CoordinateSystemControl ),
				new PropertyMetadata( 100.0, SizeChangedCallback ) );
		}
		private static void SizeChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var coordinateSystemControl = ( CoordinateSystemControl )d;
			coordinateSystemControl.Width = ( double )e.NewValue;
			coordinateSystemControl.Height = ( double )e.NewValue;
			coordinateSystemControl.UpdateCameraDistance();
		}

		public CoordinateSystemControl()
		{
			// TODO: Opacity применить к материалам дочерних GeometryModel, а не к Viewport.

			m_viewport = new Viewport3D();

			m_cameraController = new CameraController( m_viewport );

			var lights = CreateLights();
			m_viewport.Children.Add( lights );

			m_coordinateSystem = new CoordinateSystem();
			m_viewport.Children.Add( m_coordinateSystem );

			this.Content = m_viewport;

			SetBindings();

			UpdateCameraDistance();
		}
		private ModelVisual3D CreateLights()
		{
			var ambientLight = new AmbientLight( Colors.White );

			var lightsGroup = new Model3DGroup();
			lightsGroup.Children.Add( ambientLight );

			var lightsVisual = new ModelVisual3D();
			lightsVisual.Content = lightsGroup;

			return lightsVisual;
		}

		private void SetBindings()
		{
			BindingOperations.SetBinding( m_coordinateSystem, CoordinateSystem.CameraMatrixProperty, new Binding
			{
				Source = m_cameraController,
				Path = new PropertyPath( nameof( CameraController.CameraMatrix ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );
		}

		private void UpdateCameraDistance()
		{
			// Область обзора камеры на модель образует равнобедренный треугольник.
			// Расстояние до модели в данном случае - высота равнобедренного треугольника.
			// Делим равнобедренный треугольник на два прямоуголных треугольника.
			// Знаем катет a прямоугольного треугольника (половина ширины модели) и его противоположный угол А (половина угла обзора камеры).
			// Второй катет b будет высотой исходного равнобедренного треугольника.
			// Рассчитывается по формуле: b = a / tg (A).

			var a = Size / 2;
			var A = m_cameraController.Camera.FieldOfView / 2;
			var cameraDistance = a / Math.Tan( A );

			var ratio = m_coordinateSystem.DiagonalLength / Size;
			cameraDistance *= ratio;

			var currentLookDirection = m_cameraController.LookDirection;
			currentLookDirection.Normalize();
			m_cameraController.LookDirection = currentLookDirection * cameraDistance;
			//m_cameraController.Position = m_cameraController.RotateAround - m_cameraController.LookDirection;


			//m_cameraController.Distance = cameraDistance;
			//m_cameraController.Position = m_cameraController.RotateAround - m_cameraController.LookDirection * cameraDistance;
		}
	}

	/*internal class CoordinateSystemControl : UserControl
	{
		private readonly Point3D m_center = new Point3D( 0, 0, 0 );

		private CoordinateSystemVisual3D m_coordinateView;
		private Viewport3D m_viewport;

		private CameraController m_cameraController;
		public CameraController CameraController { get { return m_cameraController; } }

		public BillboardTextVisual3D XBillboardText { get { return m_coordinateView.XBillboard; } }
		public BillboardTextVisual3D YBillboardText { get { return m_coordinateView.YBillboard; } }
		public BillboardTextVisual3D ZBillboardText { get { return m_coordinateView.ZBillboard; } }

		public CoordinateSystemControl( CoordinateSystemParams coordinateViewParams ) : base()
		{
			m_viewport = new Viewport3D();
			m_viewport.Width = coordinateViewParams.ViewportSize;
			m_viewport.Height = coordinateViewParams.ViewportSize;
			m_viewport.Margin = new Thickness( coordinateViewParams.Padding );
			m_viewport.HorizontalAlignment = coordinateViewParams.HorizontalAlignment;
			m_viewport.VerticalAlignment = coordinateViewParams.VerticalAlignment;
			m_viewport.Opacity = coordinateViewParams.Opacity;

			InitCamera( coordinateViewParams.ViewportSize );
			InitLights();

			this.Content = m_viewport;

			m_coordinateView = new CoordinateSystemVisual3D(); // CoordinateSystem( coordinateViewParams );
			using( new SuspendRender( m_coordinateView ) )
			{
				m_coordinateView.ViewportSize = coordinateViewParams.ViewportSize;
			}
			m_coordinateView.Freeze();

			m_viewport.Children.Add( m_coordinateView );
		}

		private void InitCamera( double viewportWidth )
		{
			var camera = new PerspectiveCamera();

			camera.NearPlaneDistance = double.Epsilon;
			camera.FarPlaneDistance = double.PositiveInfinity;

			m_viewport.Camera = camera;

			// Область обзора камеры на модель образует равнобедренный треугольник.
			// Расстояние до модели в данном случае - высота равнобедренного треугольника.
			// Делим равнобедренный треугольник на два прямоуголных треугольника.
			// Знаем катет a прямоугольного треугольника (половина ширины модели) и его противоположный угол А (половина угла обзора камеры).
			// Второй катет b будет высотой исходного равнобедренного треугольника.
			// Рассчитывается по формуле: b = a / tg (A).

			var a = viewportWidth / 2;
			var A = camera.FieldOfView / 2;
			var b = a / Math.Tan( A );

			var cameraDistance = b;

			m_cameraController = new CameraController( camera, m_center, cameraDistance, cameraDistance );
		}

		private void InitLights()
		{
			var ambientLight = new AmbientLight( Colors.White );

			var lightsGroup = new Model3DGroup();
			lightsGroup.Children.Add( ambientLight );

			var lightsVisual = new ModelVisual3D();
			lightsVisual.Content = lightsGroup;

			m_viewport.Children.Add( lightsVisual );
		}
	}
	*/
}
