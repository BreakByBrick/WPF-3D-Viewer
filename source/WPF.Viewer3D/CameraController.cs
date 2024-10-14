using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public class CameraController : DependencyObject
	{
		private const double ZOOM_MOUSE_DELTA_LIMIT = 0.5;
		private const double ZOOM_SENSITIVITY_LIMIT = 2.0;
		private const double ZOOM_DISTANCE_MAX_RATIO = 50;
		private const double ZOOM_DISTANCE_MIN_RATIO = 1;


		private Viewport3D m_viewport;
		private CameraAnimation m_cameraAnimation;


		private double m_animationDelay = 500;
		public double AnimationDelay
		{
			get { return m_animationDelay; }
			set
			{
				if( value >= 0 )
				{
					m_animationDelay = value;
				}
			}
		}


		private PerspectiveCamera m_camera;
		public PerspectiveCamera Camera
		{
			get { return m_camera; }
		}


		private double m_zoomSensitivity = 0.1;
		public double ZoomSensitivity
		{
			get { return m_zoomSensitivity; }
			set { m_zoomSensitivity = value; }
		}


		private double m_rotateSensitivity = 0.5;
		public double RotateSensitivity
		{
			get { return m_rotateSensitivity; }
			set { m_rotateSensitivity = value; }
		}


		private Point3D m_rotateAround = new Point3D( 0, 0, 0 );
		public Point3D RotateAround
		{
			get { return m_rotateAround; }
			set { m_rotateAround = value; }
		}


		private double m_minDistance = 1.0;
		public double MinDistance
		{
			get { return m_minDistance; }
			set { m_minDistance = value; }
		}


		public Vector3D LookDirection
		{
			get { return m_camera.LookDirection; }
			set { MoveToLookDirection = value; m_camera.Position = m_rotateAround - value; }
		}

		public Vector3D UpDirection
		{
			get { return m_camera.UpDirection; }
			set { MoveToUpDirection = value; }
		}

		public Vector3D AnimateToLookDirection
		{
			get { return ( Vector3D )GetValue( AnimateToLookDirectionProperty ); }
			set { SetValue( AnimateToLookDirectionProperty, value ); }
		}
		public static readonly DependencyProperty AnimateToLookDirectionProperty;


		public Vector3D MoveToLookDirection
		{
			get { return ( Vector3D )GetValue( MoveToLookDirectionProperty ); }
			set { SetValue( MoveToLookDirectionProperty, value ); }
		}
		public static readonly DependencyProperty MoveToLookDirectionProperty;


		public Vector3D AnimateToUpDirection
		{
			get { return ( Vector3D )GetValue( AnimateToUpDirectionProperty ); }
			set { SetValue( AnimateToUpDirectionProperty, value ); }
		}
		public static readonly DependencyProperty AnimateToUpDirectionProperty;


		public Vector3D MoveToUpDirection
		{
			get { return ( Vector3D )GetValue( MoveToUpDirectionProperty ); }
			set { SetValue( MoveToUpDirectionProperty, value ); }
		}
		public static readonly DependencyProperty MoveToUpDirectionProperty;


		public Matrix3D CameraMatrix
		{
			get { return ( Matrix3D )GetValue( CameraMatrixProperty ); }
			set { SetValue( CameraMatrixProperty, value ); }
		}
		public static readonly DependencyProperty CameraMatrixProperty;


		static CameraController()
		{
			AnimateToLookDirectionProperty = DependencyProperty.Register(
				nameof( AnimateToLookDirection ),
				typeof( Vector3D ),
				typeof( CameraController ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ), AnimateToLookDirectionChangedCallback ) );

			MoveToLookDirectionProperty = DependencyProperty.Register(
				nameof( MoveToLookDirection ),
				typeof( Vector3D ),
				typeof( CameraController ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ), MoveToLookDirectionChangedCallback ) );

			AnimateToUpDirectionProperty = DependencyProperty.Register(
				nameof( AnimateToUpDirection ),
				typeof( Vector3D ),
				typeof( CameraController ),
				new PropertyMetadata( new Vector3D( 0, 1, 0 ), AnimateToUpDirectionChangedCallback ) );

			MoveToUpDirectionProperty = DependencyProperty.Register(
				nameof( MoveToUpDirection ),
				typeof( Vector3D ),
				typeof( CameraController ),
				new PropertyMetadata( new Vector3D( 0, 1, 0 ), MoveToUpDirectionChangedCallback ) );

			CameraMatrixProperty = DependencyProperty.Register(
				nameof( CameraMatrix ),
				typeof( Matrix3D ),
				typeof( CameraController ),
				new PropertyMetadata( Matrix3D.Identity ) );
		}

		private static void MoveToUpDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var cameraController = ( CameraController )d;
			var upDirection = ( Vector3D )e.NewValue;
			cameraController.AnimateUpDirection( upDirection, 0 );
		}
		private static void AnimateToUpDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var cameraController = ( CameraController )d;
			var upDirection = ( Vector3D )e.NewValue;
			cameraController.AnimateUpDirection( upDirection, cameraController.m_animationDelay );
		}
		private static void MoveToLookDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var cameraController = ( CameraController )d;
			var lookDirection = ( Vector3D )e.NewValue;
			cameraController.AnimateLookDirection( lookDirection, 0 );
		}
		private static void AnimateToLookDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var cameraController = ( CameraController )d;
			var lookDirection = ( Vector3D )e.NewValue;
			cameraController.AnimateLookDirection( lookDirection, cameraController.m_animationDelay );
		}


		public void AnimateLookDirection( Vector3D lookDirection, double animationDelay )
		{
			if( !m_cameraAnimation.IsCompleted )
				return;

			m_cameraAnimation.ToLookDirection( m_rotateAround, lookDirection, animationDelay );
			CameraMatrix = m_camera.GetViewMatrix();
		}
		public void AnimateUpDirection( Vector3D upDirection, double animationDelay )
		{
			//if( m_camera.LookDirection == upDirection || m_camera.LookDirection == -upDirection )
			//{
			//	var cUpDirection = m_camera.UpDirection;
			//	cUpDirection.Normalize();

			//	if( upDirection == cUpDirection )
			//	{
			//		upDirection = Vector3D.CrossProduct( m_camera.LookDirection, upDirection );
			//	}
			//	else
			//	{
			//		upDirection = m_camera.UpDirection;
			//	}
			//}

			m_cameraAnimation.ToUpDirection( m_rotateAround, upDirection, animationDelay );
			CameraMatrix = m_camera.GetViewMatrix();
		}


		public CameraController( Viewport3D viewport )
		{
			m_viewport = viewport;

			m_camera = m_viewport.Camera as PerspectiveCamera;
			if( m_camera == null || m_camera.IsFrozen )
			{
				m_viewport.Camera = m_camera = new PerspectiveCamera
				{
					NearPlaneDistance = 0.001,
					FarPlaneDistance = double.PositiveInfinity
				};
			}

			m_cameraAnimation = new CameraAnimation( m_camera );
		}


		public void Zoom( double mouseDelta )
		{
			// Нормализация значения изменения колесика мыши.
			if( mouseDelta < -ZOOM_MOUSE_DELTA_LIMIT )
			{
				mouseDelta = -ZOOM_MOUSE_DELTA_LIMIT;
			}
			else if( mouseDelta > ZOOM_MOUSE_DELTA_LIMIT )
			{
				mouseDelta = ZOOM_MOUSE_DELTA_LIMIT;
			}

			// Нормализация значения параметра чувствительности приближения/отдаления.
			var sensitivity = 1 + m_zoomSensitivity;
			if( sensitivity > ZOOM_SENSITIVITY_LIMIT )
			{
				sensitivity = ZOOM_SENSITIVITY_LIMIT;
			}

			// Расчет коэффициента изменения дистанции от местоположения камеры до центра модели.
			var distanceDelta = Math.Pow( sensitivity, -mouseDelta );

			Vector3D cameraRelatedPosition = m_rotateAround - m_camera.Position;
			Vector3D newCameraRelatedPosition = cameraRelatedPosition * distanceDelta;

			// Накладываем ограничение на дистанцию расположения камеры от центра модели.
			// Отменяем приближение/отдаление, если новая позиция не соответствует 
			// ограничениям по минимальной/максимальной дистанции до центра модели.
			var minDistance = MinDistance * ZOOM_DISTANCE_MIN_RATIO;
			var maxDistance = MinDistance * ZOOM_DISTANCE_MAX_RATIO;

			var curDistance = cameraRelatedPosition.Length;
			var newDistance = newCameraRelatedPosition.Length;

			if( ( ( newDistance < minDistance ) && ( curDistance >= newDistance ) )
				|| ( ( newDistance > maxDistance ) && ( curDistance <= newDistance ) ) )
				return;

			m_camera.Position = m_rotateAround - newCameraRelatedPosition;
		}
		public void Rotate( Vector delta )
		{
			if( !m_cameraAnimation.IsCompleted )
				return;

			Vector3D cameraUpDirection = m_camera.UpDirection; // !
			Vector3D cameraLookDirection = m_camera.LookDirection;
			cameraLookDirection.Normalize();

			// Получаем векторное произведение векторов направления и наклона камеры.
			// Результат - вектор, перпендикулярный векторам множителям, длина которого равна площади параллелограмма, образуемого векторами.
			Vector3D rotationDirection = Vector3D.CrossProduct( cameraLookDirection, cameraUpDirection );
			rotationDirection.Normalize();

			double d = -0.5 * m_rotateSensitivity;

			var q1 = new Quaternion( cameraUpDirection, d * delta.X );
			var q2 = new Quaternion( rotationDirection, d * delta.Y );
			Quaternion q = q1 * q2;

			var m = new Matrix3D();
			m.Rotate( q );


			Vector3D newUpDirection = m.Transform( m_camera.UpDirection );

			Vector3D relativePosition = m_rotateAround - m_camera.Position;
			Vector3D newRelativePosition = m.Transform( relativePosition );
			Point3D newPosition = m_rotateAround - newRelativePosition;

			Vector3D relativeTarget = m_rotateAround - ( m_camera.Position + m_camera.LookDirection );
			Vector3D newRelativeTarget = m.Transform( relativeTarget );
			Point3D newTarget = m_rotateAround - newRelativeTarget;
			Vector3D newLookDirection = newTarget - newPosition;

			//m_cameraAnimation.Move( newPosition, newLookDirection, newUpDirection );
			//m_cameraAnimation.Invoke( newPosition, newLookDirection, newUpDirection, 0 );
			// CameraHelper.MoveTo( m_camera, newPosition, newLookDirection, newUpDirection );
			MoveToLookDirection = newLookDirection;
			MoveToUpDirection = newUpDirection;
		}
	}

	internal class CameraAnimation
	{
		private PerspectiveCamera m_camera;

		Point3DAnimation m_positionAnimation;
		Vector3DAnimation m_lookDirectionAnimation;
		Vector3DAnimation m_upDirectionAnimation;

		private List<CameraState> m_cameraStates;
		private int m_cameraStateIndex;

		private int m_positionAnimated;
		private int m_lookDirectionAnimated;
		public bool IsCompleted
		{
			get
			{
				return m_positionAnimated >= m_cameraStates.Count
					&& m_lookDirectionAnimated >= m_cameraStates.Count;
			}
		}

		public CameraAnimation( PerspectiveCamera camera )
		{
			m_camera = camera;
			ResetLookDirectionAnimationStates();
		}
		private void ResetLookDirectionAnimationStates()
		{
			m_cameraStates = new List<CameraState>();
			m_cameraStateIndex = 0;

			m_positionAnimated = 0;
			m_lookDirectionAnimated = 0;
		}

		public void Move( Point3D toPosition, Vector3D toLookDirection, Vector3D toUpDirection )
		{
			if( !IsCompleted )
				return;

			if( toPosition == null )
				throw new ArgumentNullException( "toPosition" );

			if( toLookDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			if( toUpDirection == null )
				throw new ArgumentNullException( "toUpDirection" );

			m_camera.Position = toPosition;
			m_camera.LookDirection = toLookDirection;
			m_camera.UpDirection = toUpDirection;
		}

		public void ToLookDirection( Point3D aroundPoint, Vector3D toLookDirection, double animationDelay )
		{
			if( !IsCompleted )
				return;

			if( aroundPoint == null )
				throw new ArgumentNullException( "aroundPoint" );

			if( toLookDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			ResetLookDirectionAnimationStates();

			toLookDirection.Normalize();

			var distanceBetweenCameraAndTarget = ( m_camera.Position - aroundPoint ).Length;
			toLookDirection *= distanceBetweenCameraAndTarget;
			var toPosition = aroundPoint - toLookDirection;

			if( animationDelay > 0 )
			{
				var vectorBetweenPoints = toPosition - m_camera.Position;
				var distance = vectorBetweenPoints.Length;
				vectorBetweenPoints.Normalize();

				var partsCount = Vector3D.AngleBetween( m_camera.LookDirection, toLookDirection ) / 3;
				var partAnimationDelay = animationDelay / partsCount;
				var partDistance = distance / partsCount;

				double curDistance = partDistance;
				while( curDistance < distance )
				{
					Vector3D curVector = vectorBetweenPoints * curDistance;
					Point3D curPoint = m_camera.Position + curVector;

					Vector3D curLookDirection = aroundPoint - curPoint;
					curLookDirection.Normalize();
					curLookDirection *= distanceBetweenCameraAndTarget;

					if( double.IsNaN( curLookDirection.Length ) || curLookDirection.Length == 0 )
					{
						curDistance += partDistance;
						continue;
					}

					Point3D curPosition = aroundPoint - curLookDirection;

					m_cameraStates.Add( new CameraState()
					{
						Position = curPosition,
						LookDirection = curLookDirection,
						UpDirection = m_camera.UpDirection,
						AnimationDelay = partAnimationDelay
					} );

					curDistance += partDistance;
				}
				if( m_cameraStates.Count > 0 )
				{
					AnimatePositionAndLookDirection();
				}
			}

			m_camera.Position = toPosition;
			m_camera.LookDirection = toLookDirection;
		}
		public void ToUpDirection( Point3D aroundPoint, Vector3D toUpDirection, double animationDelay )
		{
			if( aroundPoint == null )
				throw new ArgumentNullException( "aroundPoint" );

			if( toUpDirection == null )
				throw new ArgumentNullException( "toUpDirection" );

			toUpDirection.Normalize();

			AnimateUpDirection( m_camera.UpDirection, toUpDirection, animationDelay );

			m_camera.UpDirection = toUpDirection;
		}

		public void MoveAround( Point3D aroundPoint, Vector3D toLookDirection, Vector3D toUpDirection, double animationDelay )
		{
			if( !IsCompleted )
				return;

			if( aroundPoint == null )
				throw new ArgumentNullException( "aroundPoint" );

			if( toLookDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			if( toUpDirection == null )
				throw new ArgumentNullException( "toUpDirection" );

			ResetLookDirectionAnimationStates();

			toLookDirection.Normalize();
			toUpDirection.Normalize();

			var cLookDirection = m_camera.LookDirection;
			cLookDirection.Normalize();

			if( toLookDirection == toUpDirection || toLookDirection == -toUpDirection )
			{
				var cUpDirection = m_camera.UpDirection;
				cUpDirection.Normalize();

				if( toUpDirection == cUpDirection )
				{
					toUpDirection = Vector3D.CrossProduct( m_camera.LookDirection, toUpDirection );
				}
				else
				{
					toUpDirection = m_camera.UpDirection;
				}
			}

			//if( toLookDirection == -m_camera.LookDirection )
			//{
			//	if( toLookDirection.X != 0 )
			//	{
			//		if( toLookDirection.Y != 0 )
			//		{
			//			toLookDirection = new Vector3D( 0, 0, -1 );
			//		}
			//		else
			//		{
			//			toLookDirection = new Vector3D( 0, 1, 0 );
			//		}
			//	}
			//	else
			//	{
			//		toLookDirection = new Vector3D( 1, 0, 0 );
			//	}
			//}

			// -----------------------
			var distanceBetweenCameraAndTarget = ( m_camera.Position - aroundPoint ).Length;
			toLookDirection *= distanceBetweenCameraAndTarget;
			var toPosition = aroundPoint - toLookDirection;

			if( animationDelay > 0 )
			{
				var vectorBetweenPoints = toPosition - m_camera.Position;
				var distance = vectorBetweenPoints.Length;
				vectorBetweenPoints.Normalize();

				var partsCount = Vector3D.AngleBetween( m_camera.LookDirection, toLookDirection ) / 3;
				var partAnimationDelay = animationDelay / partsCount;
				var partDistance = distance / partsCount;

				double curDistance = partDistance;
				while( curDistance < distance )
				{
					Vector3D curVector = vectorBetweenPoints * curDistance;
					Point3D curPoint = m_camera.Position + curVector;

					Vector3D curLookDirection = aroundPoint - curPoint;
					curLookDirection.Normalize();
					curLookDirection *= distanceBetweenCameraAndTarget;

					if( double.IsNaN( curLookDirection.Length ) || curLookDirection.Length == 0 )
					{
						curDistance += partDistance;
						continue;
					}

					Point3D curPosition = aroundPoint - curLookDirection;

					m_cameraStates.Add( new CameraState()
					{
						Position = curPosition,
						LookDirection = curLookDirection,
						UpDirection = m_camera.UpDirection,
						AnimationDelay = partAnimationDelay
					} );

					curDistance += partDistance;
				}
				if( m_cameraStates.Count > 0 )
				{
					AnimatePositionAndLookDirection();
					AnimateUpDirection( m_camera.UpDirection, toUpDirection, animationDelay * 2 );
				}
			}

			m_camera.Position = toPosition;
			m_camera.LookDirection = toLookDirection;
			m_camera.UpDirection = toUpDirection;

			//-----------------------------
			//var distanceBetweenCameraAndTarget = ( m_camera.Position - aroundPoint ).Length;
			//toLookDirection *= distanceBetweenCameraAndTarget;
			//var toPosition = aroundPoint - toLookDirection;

			//if( animationDelay > 0 )
			//{
			//	var vectors = new List<KeyValuePair<Vector3D, Point3D>>();
			//	var vectorBetweenPoints = toPosition - m_camera.Position;
			//	if( Vector3D.AngleBetween( m_camera.LookDirection, vectorBetweenPoints ) == 0 )
			//	{
			//		Vector3D t;
			//		if( toLookDirection.X != 0 )
			//		{
			//			if( toLookDirection.Y != 0 )
			//			{
			//				t = new Vector3D( 0, 0, -1 );
			//			}
			//			else
			//			{
			//				t = new Vector3D( 0, 1, 0 );
			//			}
			//		}
			//		else
			//		{
			//			t = new Vector3D( 1, 0, 0 );
			//		}

			//		var point = aroundPoint - t;

			//		var vector1 = point - m_camera.Position;
			//		var vector2 = toPosition - point;

			//		vectors.Add( new KeyValuePair<Vector3D, Point3D>( vector1, m_camera.Position ) );
			//		vectors.Add( new KeyValuePair<Vector3D, Point3D>( vector2, point ) );
			//	}
			//	else
			//	{
			//		vectors.Add( new KeyValuePair<Vector3D, Point3D>( vectorBetweenPoints, m_camera.Position ) );
			//	}

			//	AnimateByVectors( aroundPoint, vectors, animationDelay / vectors.Count );
			//	AnimateUpDirection( m_camera.UpDirection, toUpDirection, animationDelay );
			//}

			//m_camera.Position = toPosition;
			//m_camera.LookDirection = toLookDirection;
			//m_camera.UpDirection = toUpDirection;
		}

		private void AnimateByVectors( Point3D aroundPoint, List<KeyValuePair<Vector3D, Point3D>> vectors, double animationDelay )
		{
			var distanceBetweenCameraAndTarget = ( m_camera.Position - aroundPoint ).Length;

			foreach( var vector in vectors )
			{
				var distance = vector.Key.Length;
				vector.Key.Normalize();

				var partsCount = distance / 0.01;
				if( partsCount > 50 ) partsCount = 50;
				else if( partsCount < 10 ) partsCount = 10;

				var partAnimationDelay = animationDelay / partsCount;
				var partDistance = distance / partsCount;

				double curDistance = partDistance;
				while( curDistance < distance )
				{
					Vector3D curVector = vector.Key * curDistance;
					Point3D curPoint = vector.Value/*m_camera.Position*/ + curVector;

					Vector3D curLookDirection = aroundPoint - curPoint;
					curLookDirection.Normalize();
					curLookDirection *= distanceBetweenCameraAndTarget;

					Point3D curPosition = aroundPoint - curLookDirection;

					m_cameraStates.Add( new CameraState()
					{
						Position = curPosition,
						LookDirection = curLookDirection,
						UpDirection = m_camera.UpDirection,
						AnimationDelay = partAnimationDelay
					} );

					curDistance += partDistance;
				}
			}

			if( m_cameraStates.Count > 0 )
			{
				AnimatePositionAndLookDirection();
			}
		}

		private void AnimatePositionAndLookDirection()
		{
			var toState = m_cameraStates[ m_cameraStateIndex ];
			var fromState = m_cameraStateIndex == 0
				? new CameraState() { Position = m_camera.Position, LookDirection = m_camera.LookDirection, UpDirection = m_camera.UpDirection }
				: m_cameraStates[ m_cameraStateIndex - 1 ];

			AnimatePositionAndLookDirection( fromState, toState );
		}
		private void AnimatePositionAndLookDirection( CameraState fromState, CameraState toState )
		{
			AnimatePosition( fromState.Position, toState.Position, toState.AnimationDelay );
			AnimateLookDirection( fromState.LookDirection, toState.LookDirection, toState.AnimationDelay );
		}

		private void AnimatePosition( Point3D fromPosition, Point3D toPosition, double animationDelay )
		{
			if( animationDelay <= 0 || fromPosition == null
				|| double.IsNaN( fromPosition.X ) || double.IsNaN( fromPosition.Y ) || double.IsNaN( fromPosition.Z )
				|| toPosition == null
				|| double.IsNaN( toPosition.X ) || double.IsNaN( toPosition.Y ) || double.IsNaN( toPosition.Z )
				|| fromPosition.Equals( toPosition ) )
			{
				OnPositionAnimated();
				return;
			}

			if( m_positionAnimation == null )
			{
				m_positionAnimation = new Point3DAnimation();
				m_positionAnimation.Completed += ( s, a ) =>
				{
					m_camera.BeginAnimation( ProjectionCamera.PositionProperty, null );
					OnPositionAnimated();
				};
			}

			m_positionAnimation.From = fromPosition;
			m_positionAnimation.To = toPosition;
			m_positionAnimation.Duration = new Duration( TimeSpan.FromMilliseconds( animationDelay ) );
			m_positionAnimation.FillBehavior = FillBehavior.Stop;

			m_camera.BeginAnimation( ProjectionCamera.PositionProperty, m_positionAnimation );
		}
		private void AnimateLookDirection( Vector3D fromLookDirection, Vector3D toLookDirection, double animationDelay )
		{
			if( animationDelay <= 0
				|| fromLookDirection == null || double.IsNaN( fromLookDirection.Length )
				|| toLookDirection == null || double.IsNaN( toLookDirection.Length )
				|| fromLookDirection.Equals( toLookDirection ) )
			{
				OnLookDirectionAnimated();
				return;
			}

			if( m_lookDirectionAnimation == null )
			{
				m_lookDirectionAnimation = new Vector3DAnimation();
				m_lookDirectionAnimation.Completed += ( s, a ) =>
				{
					m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, null );
					OnLookDirectionAnimated();
				};
			}

			m_lookDirectionAnimation.From = fromLookDirection;
			m_lookDirectionAnimation.To = toLookDirection;
			m_lookDirectionAnimation.Duration = new Duration( TimeSpan.FromMilliseconds( animationDelay ) );
			m_lookDirectionAnimation.FillBehavior = FillBehavior.Stop;

			m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, m_lookDirectionAnimation );
		}
		private void AnimateUpDirection( Vector3D fromUpDirection, Vector3D toUpDirection, double animationDelay )
		{
			if( animationDelay <= 0
				|| fromUpDirection == null || double.IsNaN( fromUpDirection.Length )
				|| toUpDirection == null || double.IsNaN( toUpDirection.Length )
				|| fromUpDirection.Equals( toUpDirection ) )
			{
				return;
			}

			if( m_upDirectionAnimation == null )
			{
				m_upDirectionAnimation = new Vector3DAnimation();
				m_upDirectionAnimation.Completed += ( s, a ) =>
				{
					m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, null );
				};
			}

			m_upDirectionAnimation.From = fromUpDirection;
			m_upDirectionAnimation.To = toUpDirection;
			m_upDirectionAnimation.Duration = new Duration( TimeSpan.FromMilliseconds( animationDelay ) );
			m_upDirectionAnimation.FillBehavior = FillBehavior.Stop;

			m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, m_upDirectionAnimation );
		}


		private void OnPositionAnimated()
		{
			m_positionAnimated++;
			OnAnimated();
		}
		private void OnLookDirectionAnimated()
		{
			m_lookDirectionAnimated++;
			OnAnimated();
		}

		private void OnAnimated()
		{
			if( IsCompleted )
				return;

			if( m_lookDirectionAnimated == m_positionAnimated )
			{
				m_cameraStateIndex++;
				AnimatePositionAndLookDirection();
			}
		}

		//private void AnimatePosition( Point3D position, double animationDelay )
		//{
		//	if( position == null || position == m_camera.Position )
		//		return;

		//	if( animationDelay > 0 )
		//	{
		//		m_positionAnimationCompleted = false;

		//		var animation = new Point3DAnimation( m_camera.Position, position, new Duration( TimeSpan.FromMilliseconds( animationDelay ) ) )
		//		{
		//			AccelerationRatio = 0.3,
		//			DecelerationRatio = 0.3,
		//			FillBehavior = FillBehavior.Stop
		//		};

		//		animation.Completed += ( s, a ) =>
		//		{
		//			m_camera.BeginAnimation( ProjectionCamera.PositionProperty, null );
		//			m_positionAnimationCompleted = true;
		//		};

		//		m_camera.BeginAnimation( ProjectionCamera.PositionProperty, animation );
		//	}

		//	m_camera.Position = position;
		//}
		//private void AnimateLookDirection( Vector3D lookDirection, double animationDelay )
		//{
		//	if( lookDirection == null || lookDirection == m_camera.LookDirection )
		//		return;

		//	if( animationDelay > 0 )
		//	{
		//		m_lookDirectionAnimationCompleted = false;

		//		var animation = new Vector3DAnimation( m_camera.LookDirection, lookDirection, new Duration( TimeSpan.FromMilliseconds( animationDelay ) ) )
		//		{
		//			AccelerationRatio = 0.3,
		//			DecelerationRatio = 0.3,
		//			FillBehavior = FillBehavior.Stop
		//		};

		//		animation.Completed += ( s, a ) =>
		//		{
		//			m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, null );
		//			m_lookDirectionAnimationCompleted = true;
		//		};

		//		m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, animation );
		//	}

		//	m_camera.LookDirection = lookDirection;
		//}
		//private void AnimateUpDirection( Vector3D upDirection, double animationDelay )
		//{
		//	if( upDirection == null || upDirection == m_camera.UpDirection )
		//		return;

		//	if( animationDelay > 0 )
		//	{
		//		m_upDirectionAnimationCompleted = false;

		//		var animation = new Vector3DAnimation( m_camera.UpDirection, upDirection, new Duration( TimeSpan.FromMilliseconds( animationDelay ) ) )
		//		{
		//			AccelerationRatio = 0.3,
		//			DecelerationRatio = 0.3,
		//			FillBehavior = FillBehavior.Stop
		//		};

		//		animation.Completed += ( s, a ) =>
		//		{
		//			m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, null );
		//			m_upDirectionAnimationCompleted = true;
		//		};

		//		m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, animation );
		//	}

		//	m_camera.UpDirection = upDirection;
		//}


		//private void Animate()
		//{
		//	var cameraState = m_cameraStates[ m_cameraStateIndex ];
		//	var prevCameraState = m_cameraStateIndex == 0
		//		? new CameraState() { Position = m_camera.Position, LookDirection = m_camera.LookDirection, UpDirection = m_camera.UpDirection }
		//		: m_cameraStates[ m_cameraStateIndex - 1 ];



		//	var positionAnimation = new Point3DAnimation( prevCameraState.Position, cameraState.Position, new Duration( TimeSpan.FromMilliseconds( cameraState.AnimationDelay ) ) )
		//	{
		//		AccelerationRatio = 0.3,
		//		DecelerationRatio = 0.3,
		//		FillBehavior = FillBehavior.Stop
		//	};

		//	positionAnimation.Completed += ( s, a ) =>
		//	{
		//		m_camera.BeginAnimation( ProjectionCamera.PositionProperty, null );
		//		OnPositionAnimated();
		//	};

		//	m_camera.BeginAnimation( ProjectionCamera.PositionProperty, positionAnimation );







		//	var lookDirectionAnimation = new Vector3DAnimation( prevCameraState.LookDirection, cameraState.LookDirection, new Duration( TimeSpan.FromMilliseconds( cameraState.AnimationDelay ) ) )
		//	{
		//		AccelerationRatio = 0.3,
		//		DecelerationRatio = 0.3,
		//		FillBehavior = FillBehavior.Stop
		//	};

		//	lookDirectionAnimation.Completed += ( s, a ) =>
		//	{
		//		m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, null );
		//		OnLookDirectionAnimated();
		//	};

		//	m_camera.BeginAnimation( ProjectionCamera.LookDirectionProperty, lookDirectionAnimation );






		//	var upDirectionAnimation = new Vector3DAnimation( prevCameraState.UpDirection, cameraState.UpDirection, new Duration( TimeSpan.FromMilliseconds( cameraState.AnimationDelay ) ) )
		//	{
		//		AccelerationRatio = 0.3,
		//		DecelerationRatio = 0.3,
		//		FillBehavior = FillBehavior.Stop
		//	};

		//	upDirectionAnimation.Completed += ( s, a ) =>
		//	{
		//		m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, null );
		//		OnUpDirectionAnimated();
		//	};

		//	m_camera.BeginAnimation( ProjectionCamera.UpDirectionProperty, upDirectionAnimation );
		//}

		private struct CameraState
		{
			public Point3D Position { get; set; }
			public Vector3D LookDirection { get; set; }
			public Vector3D UpDirection { get; set; }
			public double AnimationDelay { get; set; }
		}
	}
}
