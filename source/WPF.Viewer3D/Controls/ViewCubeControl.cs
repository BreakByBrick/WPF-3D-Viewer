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
	public class ViewCubeControl : UserControl
	{
		private Viewport3D m_viewport;
		private ViewCube m_viewCube;

		private CameraController m_cameraController;
		public CameraController CameraController { get { return m_cameraController; } }

		public double Size
		{
			get { return ( double )GetValue( SizeProperty ); }
			set { SetValue( SizeProperty, value ); }
		}
		public static readonly DependencyProperty SizeProperty;

		static ViewCubeControl()
		{
			SizeProperty = DependencyProperty.Register(
				nameof( Size ),
				typeof( double ),
				typeof( ViewCubeControl ),
				new PropertyMetadata( 100.0, SizeChangedCallback ) );
		}
		private static void SizeChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCubeControl = ( ViewCubeControl )d;
			viewCubeControl.Width = ( double )e.NewValue;
			viewCubeControl.Height = ( double )e.NewValue;
			viewCubeControl.UpdateCameraDistance();
		}


		public ViewCubeControl()
		{
			// TODO: Opacity применить к материалам дочерних GeometryModel, а не к Viewport.

			m_viewport = new Viewport3D();

			m_cameraController = new CameraController( m_viewport );

			var lights = InitLights();
			m_viewport.Children.Add( lights );

			m_viewCube = new ViewCube();
			m_viewport.Children.Add( m_viewCube );

			this.Content = m_viewport;

			SetBindings();

			UpdateCameraDistance();
		}
		private ModelVisual3D InitLights()
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
			BindingOperations.SetBinding( m_cameraController, CameraController.AnimateToLookDirectionProperty, new Binding
			{
				Source = m_viewCube,
				Path = new PropertyPath( nameof( ViewCube.LookDirection ) ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			BindingOperations.SetBinding( m_cameraController, CameraController.AnimateToUpDirectionProperty, new Binding
			{
				Source = m_viewCube,
				Path = new PropertyPath( nameof( ViewCube.UpDirection ) ),
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

			var ratio = m_viewCube.DiagonalLength / Size;
			cameraDistance *= ratio;

			var currentLookDirection = m_cameraController.LookDirection;
			currentLookDirection.Normalize();
			m_cameraController.LookDirection = currentLookDirection * cameraDistance;
			//m_cameraController.Position = m_cameraController.RotateAround - m_cameraController.LookDirection;


			//m_cameraController.Distance = cameraDistance;
			//m_cameraController.Position = m_cameraController.RotateAround - m_cameraController.LookDirection * cameraDistance;
		}

		public void PickCubeFace( CubeFaceType cubeFaceType )
		{
			m_viewCube.PickCubeFace( cubeFaceType );
		}
		public void Pick( Vector3D lookDirection, Vector3D upDirection )
		{
			m_viewCube.Pick( lookDirection, upDirection );
		}
	}

	/*internal class ViewCubeControl : UserControl
	{
		private readonly Point3D m_centerPoint = new Point3D( 0, 0, 0 );

		private Viewport3D m_viewport;
		private ViewCubeVisual3D m_viewCube;
		private CameraController m_cameraController;

		public CameraController CameraController { get { return m_cameraController; } }

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

		static ViewCubeControl()
		{
			LookDirectionProperty = DependencyProperty.Register(
				"LookDirection",
				typeof( Vector3D ),
				typeof( ViewCubeControl ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ), LookDirectionChangedCallback ) );

			UpDirectionProperty = DependencyProperty.Register(
				"UpDirection",
				typeof( Vector3D ),
				typeof( ViewCubeControl ),
				new PropertyMetadata( new Vector3D( 0, 1, 0 ), UpDirectionChangedCallback ) );
		}

		private static void UpDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCubeControl = ( ViewCubeControl )d;
			//viewCubeControl.m_cameraController.AnimateUpDirection( ( Vector3D )e.NewValue, 500 );
		}
		private static void LookDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var viewCubeControl = ( ViewCubeControl )d;
			//viewCubeControl.m_cameraController.AnimateLookDirection( ( Vector3D )e.NewValue, 500 );
		}

		public ViewCubeControl( Action<Vector3D, Vector3D> viewCubeCallback, ViewCubeParams viewCubeParams ) : base()
		{
			m_viewport = new Viewport3D();
			m_viewport.Width = viewCubeParams.ViewportSize;
			m_viewport.Height = viewCubeParams.ViewportSize;
			m_viewport.Margin = new Thickness( viewCubeParams.Padding );
			m_viewport.HorizontalAlignment = viewCubeParams.HorizontalAlignment;
			m_viewport.VerticalAlignment = viewCubeParams.VerticalAlignment;
			m_viewport.Opacity = viewCubeParams.Opacity;

			InitCamera( viewCubeParams.ViewportSize );
			InitLights();

			this.Content = m_viewport;

			m_viewCube = new ViewCubeVisual3D(); // new ViewCube( viewCubeCallback, viewCubeParams );
			using( new SuspendRender( m_viewCube ) )
			{
				m_viewCube.ViewportSize = viewCubeParams.ViewportSize;
			}
			m_viewCube.Freeze();
			m_viewCube.UpdateModelAction = viewCubeCallback;
			m_viewport.Children.Add( m_viewCube );


			this.SetBinding( ViewCubeControl.LookDirectionProperty, new Binding
			{
				Source = m_viewCube,
				Path = new PropertyPath( "LookDirection" ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );

			this.SetBinding( ViewCubeControl.UpDirectionProperty, new Binding
			{
				Source = m_viewCube,
				Path = new PropertyPath( "UpDirection" ),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Mode = BindingMode.OneWay
			} );
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
			// Знаем катет a прямоугольного треугольника (половина диагонали модели) и его противоположный угол А (половина угла обзора камеры).
			// Второй катет b будет высотой исходного равнобедренного треугольника.
			// Рассчитывается по формуле: b = a / tg (A).

			// Диагональ модели - расстояние между двумя максимально удаленными точками модели (в данном случае, модели Куба просмотра).
			// За диагональ возьмем длину стороны области просмотра, т.к. фактическое значение не важно, может быть хоть 1, хоть 1000 - 
			// графический движок автоматически настраивает разрешение модели в любом случае, чтобы не было видно "пиксельности" на изображении.
			// Значение диагонали используется только чтобы синхронизировать расположение камеры с моделью (ее диагональю).
			// Размеры модели Куба просмотра также будут "привязаны" к значению этой "диагонали".

			var a = viewportWidth / 2;
			var A = camera.FieldOfView / 2;
			var b = a / Math.Tan( A );

			var cameraDistance = b;

			m_cameraController = new CameraController( camera, m_centerPoint, cameraDistance, cameraDistance );
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

		public void Pick( ViewCubeSide viewCubeSide )
		{
			//m_viewCube.PickSide( viewCubeSide );
		}
	}
	*/
}
