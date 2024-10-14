using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	public class SpotLightController : DependencyObject
	{
		private SpotLight m_spotLight;
		private SpotLightAnimation m_spotLightAnimation;

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

		private Point3D m_rotateAround = new Point3D( 0, 0, 0 );
		public Point3D RotateAround
		{
			get { return m_rotateAround; }
			set { m_rotateAround = value; }
		}


		public Vector3D LookDirection
		{
			get { return m_spotLight.Direction; }
			set { MoveToLookDirection = value; m_spotLight.Position = m_rotateAround - value; }
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


		static SpotLightController()
		{
			AnimateToLookDirectionProperty = DependencyProperty.Register(
				nameof( AnimateToLookDirection ),
				typeof( Vector3D ),
				typeof( SpotLightController ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ), AnimateToLookDirectionChangedCallback ) );

			MoveToLookDirectionProperty = DependencyProperty.Register(
				nameof( MoveToLookDirection ),
				typeof( Vector3D ),
				typeof( SpotLightController ),
				new PropertyMetadata( new Vector3D( 0, 0, 1 ), MoveToLookDirectionChangedCallback ) );
		}

		private static void MoveToLookDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var spotLightController = ( SpotLightController )d;
			var lookDirection = ( Vector3D )e.NewValue;
			spotLightController.AnimateLookDirection( lookDirection, 0 );
		}
		private static void AnimateToLookDirectionChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			var spotLightController = ( SpotLightController )d;
			var lookDirection = ( Vector3D )e.NewValue;
			spotLightController.AnimateLookDirection( lookDirection, spotLightController.m_animationDelay );
		}

		public void AnimateLookDirection( Vector3D lookDirection, double animationDelay )
		{
			if( !m_spotLightAnimation.IsCompleted )
				return;

			m_spotLightAnimation.ToLookDirection( m_rotateAround, lookDirection, animationDelay );
		}

		public SpotLightController( SpotLight spotLight )
		{
			m_spotLight = spotLight;
			m_spotLightAnimation = new SpotLightAnimation( spotLight );
		}
	}

	public class SpotLightAnimation
	{
		private SpotLight m_spotLight;

		Point3DAnimation m_positionAnimation;
		Vector3DAnimation m_directionAnimation;

		private List<SpotLightState> m_spotLightStates;
		private int m_spotLightStateIndex;

		private int m_positionAnimated;
		private int m_lookDirectionAnimated;
		public bool IsCompleted
		{
			get
			{
				return m_positionAnimated >= m_spotLightStates.Count
					&& m_lookDirectionAnimated >= m_spotLightStates.Count;
			}
		}

		public SpotLightAnimation( SpotLight spotLight )
		{
			m_spotLight = spotLight;
			ResetAnimationStates();
		}
		private void ResetAnimationStates()
		{
			m_spotLightStates = new List<SpotLightState>();
			m_spotLightStateIndex = 0;

			m_positionAnimated = 0;
			m_lookDirectionAnimated = 0;
		}

		public void Move( Point3D toPosition, Vector3D toDirection )
		{
			if( !IsCompleted )
				return;

			if( toPosition == null )
				throw new ArgumentNullException( "toPosition" );

			if( toDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			m_spotLight.Position = toPosition;
			m_spotLight.Direction = toDirection;
		}

		public void MoveAround( Point3D aroundPoint, Vector3D toDirection, double animationDelay )
		{
			if( !IsCompleted )
				return;

			if( aroundPoint == null )
				throw new ArgumentNullException( "aroundPoint" );

			if( toDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			ResetAnimationStates();

			toDirection.Normalize();

			var distanceBetweenCameraAndTarget = ( m_spotLight.Position - aroundPoint ).Length;
			toDirection *= distanceBetweenCameraAndTarget;
			var toPosition = aroundPoint - toDirection;

			if( animationDelay > 0 )
			{
				var vectorBetweenPoints = toPosition - m_spotLight.Position;
				var distance = vectorBetweenPoints.Length;
				vectorBetweenPoints.Normalize();

				var partsCount = Vector3D.AngleBetween( m_spotLight.Direction, toDirection ) / 3;
				var partAnimationDelay = animationDelay / partsCount;
				var partDistance = distance / partsCount;

				double curDistance = partDistance;
				while( curDistance < distance )
				{
					Vector3D curVector = vectorBetweenPoints * curDistance;
					Point3D curPoint = m_spotLight.Position + curVector;

					Vector3D curLookDirection = aroundPoint - curPoint;
					curLookDirection.Normalize();
					curLookDirection *= distanceBetweenCameraAndTarget;

					if( curLookDirection.Length == 0 )
					{
						curDistance += partDistance;
						continue;
					}

					Point3D curPosition = aroundPoint - curLookDirection;

					m_spotLightStates.Add( new SpotLightState()
					{
						Position = curPosition,
						Direction = curLookDirection,
						AnimationDelay = partAnimationDelay
					} );

					curDistance += partDistance;
				}
				if( m_spotLightStates.Count > 0 )
					AnimatePositionAndLookDirection();
			}

			m_spotLight.Position = toPosition;
			m_spotLight.Direction = toDirection;
		}

		public void ToLookDirection( Point3D aroundPoint, Vector3D toLookDirection, double animationDelay )
		{
			if( !IsCompleted )
				return;

			if( aroundPoint == null )
				throw new ArgumentNullException( "aroundPoint" );

			if( toLookDirection == null )
				throw new ArgumentNullException( "toLookDirection" );

			ResetAnimationStates();

			toLookDirection.Normalize();

			var distanceBetweenCameraAndTarget = ( m_spotLight.Position - aroundPoint ).Length;
			toLookDirection *= distanceBetweenCameraAndTarget;
			var toPosition = aroundPoint - toLookDirection;

			if( animationDelay > 0 )
			{
				var vectorBetweenPoints = toPosition - m_spotLight.Position;
				var distance = vectorBetweenPoints.Length;
				vectorBetweenPoints.Normalize();

				var partsCount = Vector3D.AngleBetween( m_spotLight.Direction, toLookDirection ) / 3;
				var partAnimationDelay = animationDelay / partsCount;
				var partDistance = distance / partsCount;

				double curDistance = partDistance;
				while( curDistance < distance )
				{
					Vector3D curVector = vectorBetweenPoints * curDistance;
					Point3D curPoint = m_spotLight.Position + curVector;

					Vector3D curLookDirection = aroundPoint - curPoint;
					curLookDirection.Normalize();
					curLookDirection *= distanceBetweenCameraAndTarget;

					if( double.IsNaN( curLookDirection.Length ) || curLookDirection.Length == 0 )
					{
						curDistance += partDistance;
						continue;
					}

					Point3D curPosition = aroundPoint - curLookDirection;

					m_spotLightStates.Add( new SpotLightState()
					{
						Position = curPosition,
						Direction = curLookDirection,
						AnimationDelay = partAnimationDelay
					} );

					curDistance += partDistance;
				}
				if( m_spotLightStates.Count > 0 )
				{
					AnimatePositionAndLookDirection();
				}
			}

			m_spotLight.Position = toPosition;
			m_spotLight.Direction = toLookDirection;
		}

		private void AnimatePositionAndLookDirection()
		{
			var toState = m_spotLightStates[ m_spotLightStateIndex ];
			var fromState = m_spotLightStateIndex == 0
				? new SpotLightState() { Position = m_spotLight.Position, Direction = m_spotLight.Direction }
				: m_spotLightStates[ m_spotLightStateIndex - 1 ];

			AnimatePositionAndLookDirection( fromState, toState );
		}
		private void AnimatePositionAndLookDirection( SpotLightState fromState, SpotLightState toState )
		{
			AnimatePosition( fromState.Position, toState.Position, toState.AnimationDelay );
			AnimateLookDirection( fromState.Direction, toState.Direction, toState.AnimationDelay );
		}

		private void AnimatePosition( Point3D fromPosition, Point3D toPosition, double animationDelay )
		{
			if( fromPosition == null || toPosition == null || fromPosition == toPosition || animationDelay <= 0 )
			{
				OnPositionAnimated();
				return;
			}

			if( m_positionAnimation == null )
			{
				m_positionAnimation = new Point3DAnimation();
				m_positionAnimation.Completed += ( s, a ) =>
				{
					m_spotLight.BeginAnimation( SpotLight.PositionProperty, null );
					OnPositionAnimated();
				};
			}

			m_positionAnimation.From = fromPosition;
			m_positionAnimation.To = toPosition;
			m_positionAnimation.Duration = new Duration( TimeSpan.FromMilliseconds( animationDelay ) );
			m_positionAnimation.FillBehavior = FillBehavior.Stop;

			m_spotLight.BeginAnimation( SpotLight.PositionProperty, m_positionAnimation );
		}
		private void AnimateLookDirection( Vector3D fromLookDirection, Vector3D toLookDirection, double animationDelay )
		{
			if( fromLookDirection == null || toLookDirection == null || fromLookDirection == toLookDirection || animationDelay <= 0 )
			{
				OnLookDirectionAnimated();
				return;
			}

			if( m_directionAnimation == null )
			{
				m_directionAnimation = new Vector3DAnimation();
				m_directionAnimation.Completed += ( s, a ) =>
				{
					m_spotLight.BeginAnimation( SpotLight.DirectionProperty, null );
					OnLookDirectionAnimated();
				};
			}

			m_directionAnimation.From = fromLookDirection;
			m_directionAnimation.To = toLookDirection;
			m_directionAnimation.Duration = new Duration( TimeSpan.FromMilliseconds( animationDelay ) );
			m_directionAnimation.FillBehavior = FillBehavior.Stop;

			m_spotLight.BeginAnimation( SpotLight.DirectionProperty, m_directionAnimation );
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
				m_spotLightStateIndex++;
				AnimatePositionAndLookDirection();
			}
		}

		private class SpotLightState
		{
			public Point3D Position { get; set; }
			public Vector3D Direction { get; set; }
			public double AnimationDelay { get; set; }
		}
	}
}
