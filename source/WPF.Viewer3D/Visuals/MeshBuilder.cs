using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace WPF.Viewer3D.Visuals
{
	internal class MeshBuilder : IDisposable
	{
		private bool m_isDisposed;


		private Point3DCollection m_positions;
		public Point3DCollection Positions
		{
			get { return this.m_positions; }
		}


		private Int32Collection m_triangleIndices;
		public Int32Collection TriangleIndices
		{
			get { return this.m_triangleIndices; }
		}


		private PointCollection m_textureCoordinates;
		public PointCollection TextureCoordinates
		{
			get { return this.m_textureCoordinates; }
			set { this.m_textureCoordinates = value; }
		}


		private Vector3DCollection m_normals;
		public Vector3DCollection Normals
		{
			get { return this.m_normals; }
			set { this.m_normals = value; }
		}


		private static readonly ThreadLocal<Dictionary<int, IList<Point>>> m_circleCache
			= new ThreadLocal<Dictionary<int, IList<Point>>>( () => new Dictionary<int, IList<Point>>() );

		private static readonly ThreadLocal<Dictionary<int, IList<Point>>> m_closedCircleCache
			= new ThreadLocal<Dictionary<int, IList<Point>>>( () => new Dictionary<int, IList<Point>>() );


		public MeshBuilder() : this( true, true ) { }
		public MeshBuilder( bool generateNormals = true, bool generateTexCoords = true )
		{
			this.m_positions = new Point3DCollection();
			this.m_triangleIndices = new Int32Collection();

			if( generateNormals )
			{
				this.m_normals = new Vector3DCollection();
			}

			if( generateTexCoords )
			{
				this.m_textureCoordinates = new PointCollection();
			}
		}

		~MeshBuilder()
		{
			Dispose( disposing: false );
		}


		public void Append( MeshGeometry3D mesh )
		{
			if( mesh == null )
				throw new ArgumentNullException( nameof( mesh ) );

			Vector3DCollection normals = this.m_normals != null ? mesh.Normals : null;
			PointCollection textureCoordinates = this.m_textureCoordinates != null ? mesh.TextureCoordinates : null;

			this.Append( mesh.Positions, mesh.TriangleIndices, normals, textureCoordinates );
		}
		public void Append(
			IList<Point3D> positionsToAppend, IList<int> triangleIndicesToAppend,
			IList<Vector3D> normalsToAppend = null, IList<Point> textureCoordinatesToAppend = null )
		{
			if( positionsToAppend == null )
				throw new ArgumentNullException( nameof( positionsToAppend ) );

			if( this.m_normals != null && normalsToAppend == null )
				throw new InvalidOperationException( "Cant't append mesh geometry. Source mesh don't contains normals." );

			if( normalsToAppend != null && normalsToAppend.Count != positionsToAppend.Count )
				throw new InvalidOperationException( "Cant't append mesh geometry. Source mesh contains wrong number of normals." );

			if( this.m_textureCoordinates != null && textureCoordinatesToAppend == null )
				throw new InvalidOperationException( "Cant't append mesh geometry. Source mesh don't contains texture coordinates." );

			if( textureCoordinatesToAppend != null && textureCoordinatesToAppend.Count != positionsToAppend.Count )
				throw new InvalidOperationException( "Cant't append mesh geometry. Source mesh contains wrong number of texture coordinates." );

			int index = this.m_positions.Count;
			foreach( var p in positionsToAppend )
			{
				this.m_positions.Add( p );
			}

			if( this.m_normals != null && normalsToAppend != null )
			{
				foreach( var n in normalsToAppend )
				{
					this.m_normals.Add( n );
				}
			}

			if( this.m_textureCoordinates != null && textureCoordinatesToAppend != null )
			{
				foreach( var t in textureCoordinatesToAppend )
				{
					this.m_textureCoordinates.Add( t );
				}
			}

			foreach( int i in triangleIndicesToAppend )
			{
				this.m_triangleIndices.Add( index + i );
			}
		}


		public void AddArrow( Point3D point1, Point3D point2, double diameter, double headLength = 3, int thetaDiv = 18 )
		{
			var dir = point2 - point1;
			var length = dir.Length; // Helper3D.Length( ref dir );
			var r = ( double )diameter / 2;

			var pc = new PointCollection
				{
					new Point(0, 0),
					new Point(0, r),
					new Point(length - (double)(diameter * headLength), r),
					new Point(length - (double)(diameter * headLength), r * 2),
					new Point(length, 0)
				};

			this.AddRevolvedGeometry( pc, null, point1, dir, thetaDiv );
		}
		private Vector3D FindAnyPerpendicular( Vector3D n )
		{
			n.Normalize();
			Vector3D u = Vector3D.CrossProduct( new Vector3D( 0, 1, 0 ), n );
			if( u.LengthSquared < 1e-3 )
			{
				u = Vector3D.CrossProduct( new Vector3D( 1, 0, 0 ), n );
			}

			return u;
		}
		/// <summary>
		/// Добавление вращательной геометрии.
		/// </summary>
		/// <param name="points">Точки (координаты x - это расстояние от начала координат по оси вращения, координаты y - радиус)</param>
		/// <param name="textureValues">Координаты текстуры, по одной для каждой точки в списке точек.</param>
		/// <param name="origin">Начало оси вращения.</param>
		/// <param name="direction">Направление оси вращения.</param>
		/// <param name="thetaDiv">Количество делений сетки геометрии.</param>
		public void AddRevolvedGeometry( IList<Point> points, IList<double> textureValues, Point3D origin, Vector3D direction, int thetaDiv )
		{
			direction.Normalize();

			// Рассчет двух векторов, ортогональных направлению оси вращения.
			var u = FindAnyPerpendicular( direction );
			var v = Vector3D.CrossProduct( direction, u ); //Helper3D.CrossProduct( ref direction, ref u );
			u.Normalize();
			v.Normalize();

			var circle = GetCircle( thetaDiv );

			int index0 = this.m_positions.Count;
			int n = points.Count;

			int totalNodes = ( points.Count - 1 ) * 2 * thetaDiv;
			int rowNodes = ( points.Count - 1 ) * 2;

			for( int i = 0; i < thetaDiv; i++ )
			{
				var w = ( v * circle[ i ].X ) + ( u * circle[ i ].Y );

				for( int j = 0; j + 1 < n; j++ )
				{
					var q1 = origin + ( direction * points[ j ].X ) + ( w * points[ j ].Y );
					var q2 = origin + ( direction * points[ j + 1 ].X ) + ( w * points[ j + 1 ].Y );

					this.m_positions.Add( q1 );
					this.m_positions.Add( q2 );

					if( this.m_normals != null )
					{
						var tx = points[ j + 1 ].X - points[ j ].X;
						var ty = points[ j + 1 ].Y - points[ j ].Y;
						var normal = ( -direction * ty ) + ( w * tx );
						normal.Normalize();
						this.m_normals.Add( normal );
						this.m_normals.Add( normal );
					}

					if( this.m_textureCoordinates != null )
					{
						this.m_textureCoordinates.Add( new Point( ( double )i / ( thetaDiv - 1 ), textureValues == null ? ( double )j / ( n - 1 ) : ( double )textureValues[ j ] ) );
						this.m_textureCoordinates.Add( new Point( ( double )i / ( thetaDiv - 1 ), textureValues == null ? ( double )( j + 1 ) / ( n - 1 ) : ( double )textureValues[ j + 1 ] ) );
					}

					int i0 = index0 + ( i * rowNodes ) + ( j * 2 );
					int i1 = i0 + 1;
					int i2 = index0 + ( ( ( ( i + 1 ) * rowNodes ) + ( j * 2 ) ) % totalNodes );
					int i3 = i2 + 1;

					this.m_triangleIndices.Add( i1 );
					this.m_triangleIndices.Add( i0 );
					this.m_triangleIndices.Add( i2 );

					this.m_triangleIndices.Add( i1 );
					this.m_triangleIndices.Add( i2 );
					this.m_triangleIndices.Add( i3 );
				}
			}
		}
		public static IList<Point> GetCircle( int thetaDiv, bool closed = false )
		{
			IList<Point> circle = null;

			// Если не удается найти круг в кэше.
			if( ( !closed && !m_circleCache.Value.TryGetValue( thetaDiv, out circle ) ) ||
				( closed && !m_closedCircleCache.Value.TryGetValue( thetaDiv, out circle ) ) )
			{
				circle = new PointCollection();

				if( !closed )
				{
					m_circleCache.Value.Add( thetaDiv, circle );
				}
				else
				{
					m_closedCircleCache.Value.Add( thetaDiv, circle );
				}

				var num = closed ? thetaDiv : thetaDiv - 1;
				for( int i = 0; i < thetaDiv; i++ )
				{
					var theta = ( double )Math.PI * 2 * ( ( double )i / num );
					circle.Add( new Point( ( double )Math.Cos( theta ), -( double )Math.Sin( theta ) ) );
				}
			}

			IList<Point> result = new List<Point>();
			foreach( var point in circle )
			{
				result.Add( new Point( point.X, point.Y ) );
			}
			return result;
		}


		public void AddBox( Point3D center, double xlength, double ylength, double zlength )
		{
			this.AddBox( center, xlength, ylength, zlength, BoxSide.All );
		}
		public void AddBox( Point3D center, double xlength, double ylength, double zlength, BoxSide faces )
		{
			this.AddBox( center, new Vector3D( 1, 0, 0 ), new Vector3D( 0, 1, 0 ), xlength, ylength, zlength, faces );
		}
		public void AddBox( Point3D center, Vector3D x, Vector3D y, double xlength, double ylength, double zlength, BoxSide faces = BoxSide.All )
		{
			var z = Vector3D.CrossProduct( x, y );
			if( ( faces & BoxSide.Front ) == BoxSide.Front )
			{
				this.AddCubeSide( center, x, z, xlength, ylength, zlength );
			}

			if( ( faces & BoxSide.Back ) == BoxSide.Back )
			{
				this.AddCubeSide( center, -x, z, xlength, ylength, zlength );
			}

			if( ( faces & BoxSide.Left ) == BoxSide.Left )
			{
				this.AddCubeSide( center, -y, z, ylength, xlength, zlength );
			}

			if( ( faces & BoxSide.Right ) == BoxSide.Right )
			{
				this.AddCubeSide( center, y, z, ylength, xlength, zlength );
			}

			if( ( faces & BoxSide.Top ) == BoxSide.Top )
			{
				this.AddCubeSide( center, z, y, zlength, xlength, ylength );
			}

			if( ( faces & BoxSide.Bottom ) == BoxSide.Bottom )
			{
				this.AddCubeSide( center, -z, y, zlength, xlength, ylength );
			}
		}
		public void AddCubeSide( Point3D center, Vector3D normal, Vector3D up, double dist, double width, double height )
		{
			var right = Vector3D.CrossProduct( normal, up );
			var n = normal * ( double )dist / 2;
			up *= ( double )height / 2;
			right *= ( double )width / 2;
			var p1 = center + n - up - right;
			var p2 = center + n - up + right;
			var p3 = center + n + up + right;
			var p4 = center + n + up - right;

			int i0 = this.m_positions.Count;
			this.m_positions.Add( p1 );
			this.m_positions.Add( p2 );
			this.m_positions.Add( p3 );
			this.m_positions.Add( p4 );

			if( this.m_normals != null )
			{
				this.m_normals.Add( normal );
				this.m_normals.Add( normal );
				this.m_normals.Add( normal );
				this.m_normals.Add( normal );
			}

			if( this.m_textureCoordinates != null )
			{
				this.m_textureCoordinates.Add( new Point( 1, 1 ) );
				this.m_textureCoordinates.Add( new Point( 0, 1 ) );
				this.m_textureCoordinates.Add( new Point( 0, 0 ) );
				this.m_textureCoordinates.Add( new Point( 1, 0 ) );
			}

			this.m_triangleIndices.Add( i0 + 2 );
			this.m_triangleIndices.Add( i0 + 1 );
			this.m_triangleIndices.Add( i0 + 0 );
			this.m_triangleIndices.Add( i0 + 0 );
			this.m_triangleIndices.Add( i0 + 3 );
			this.m_triangleIndices.Add( i0 + 2 );
		}

		public void AddSphere( Point3D center, double radius = 1, int thetaDiv = 32, int phiDiv = 32 )
		{
			this.AddEllipsoid( center, radius, radius, radius, thetaDiv, phiDiv );
		}
		public void AddEllipsoid( Point3D center, double radiusx, double radiusy, double radiusz, int thetaDiv = 20, int phiDiv = 10 )
		{
			int index0 = this.Positions.Count;
			var dt = 2 * ( double )Math.PI / thetaDiv;
			var dp = ( double )Math.PI / phiDiv;

			for( int pi = 0; pi <= phiDiv; pi++ )
			{
				var phi = pi * dp;

				for( int ti = 0; ti <= thetaDiv; ti++ )
				{
					var theta = ti * dt;

					var x = ( double )Math.Cos( theta ) * ( double )Math.Sin( phi );
					var y = ( double )Math.Sin( theta ) * ( double )Math.Sin( phi );
					var z = ( double )Math.Cos( phi );

					var p = new Point3D( center.X + ( double )( radiusx * x ), center.Y + ( double )( radiusy * y ), center.Z + ( double )( radiusz * z ) );
					this.m_positions.Add( p );

					if( this.m_normals != null )
					{
						var n = new Vector3D( x, y, z );
						this.m_normals.Add( n );
					}

					if( this.m_textureCoordinates != null )
					{
						var uv = new Point( theta / ( 2 * ( double )Math.PI ), phi / ( double )Math.PI );
						this.m_textureCoordinates.Add( uv );
					}
				}
			}

			this.AddRectangularMeshTriangleIndices( index0, phiDiv + 1, thetaDiv + 1, true );
		}

		public void AddPipe( Point3D point1, Point3D point2, double innerDiameter, double diameter, int thetaDiv )
		{
			var dir = point2 - point1;

			var height = Math.Sqrt( dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z );
			dir.Normalize();

			var pc = new PointCollection
				{
					new Point(0, (double)innerDiameter / 2),
					new Point(0, (double)diameter / 2),
					new Point(height, (double)diameter / 2),
					new Point(height, (double)innerDiameter / 2)
				};

			var tc = new List<double> { 1, 0, 1, 0 };

			if( innerDiameter > 0 )
			{
				// Add the inner surface
				pc.Add( new Point( 0, ( double )innerDiameter / 2 ) );
				tc.Add( 1 );
			}

			this.AddRevolvedGeometry( pc, tc, point1, dir, thetaDiv );
		}

		public void AddRectangularMesh( IList<Point3D> points, int columns )
		{
			if( points == null )
			{
				throw new ArgumentNullException( nameof( points ) );
			}

			int index0 = this.Positions.Count;

			foreach( var pt in points )
			{
				this.m_positions.Add( pt );
			}

			int rows = points.Count / columns;

			this.AddRectangularMeshTriangleIndices( index0, rows, columns );
			if( this.m_normals != null )
			{
				this.AddRectangularMeshNormals( index0, rows, columns );
			}

			if( this.m_textureCoordinates != null )
			{
				this.AddRectangularMeshTextureCoordinates( rows, columns );
			}
		}

		public void AddRectangularMeshTriangleIndices( int index0, int rows, int columns, bool isSpherical = false )
		{
			for( int i = 0; i < rows - 1; i++ )
			{
				for( int j = 0; j < columns - 1; j++ )
				{
					int ij = ( i * columns ) + j;
					if( !isSpherical || i > 0 )
					{
						this.m_triangleIndices.Add( index0 + ij );
						this.m_triangleIndices.Add( index0 + ij + 1 + columns );
						this.m_triangleIndices.Add( index0 + ij + 1 );
					}

					if( !isSpherical || i < rows - 2 )
					{
						this.m_triangleIndices.Add( index0 + ij + 1 + columns );
						this.m_triangleIndices.Add( index0 + ij );
						this.m_triangleIndices.Add( index0 + ij + columns );
					}
				}
			}
		}
		
		private void AddRectangularMeshNormals( int index0, int rows, int columns )
		{
			for( int i = 0; i < rows; i++ )
			{
				int i1 = i + 1;
				if( i1 == rows )
				{
					i1--;
				}

				int i0 = i1 - 1;
				for( int j = 0; j < columns; j++ )
				{
					int j1 = j + 1;
					if( j1 == columns )
					{
						j1--;
					}

					int j0 = j1 - 1;
					var u = Point3D.Subtract(
						this.m_positions[ index0 + ( i1 * columns ) + j0 ], this.m_positions[ index0 + ( i0 * columns ) + j0 ] );
					var v = Point3D.Subtract(
						this.m_positions[ index0 + ( i0 * columns ) + j1 ], this.m_positions[ index0 + ( i0 * columns ) + j0 ] );
					var normal = Vector3D.CrossProduct( u, v );
					normal.Normalize();
					this.m_normals.Add( normal );
				}
			}
		}
		
		private void AddRectangularMeshTextureCoordinates( int rows, int columns, bool flipRowsAxis = false, bool flipColumnsAxis = false )
		{
			for( int i = 0; i < rows; i++ )
			{
				var v = flipRowsAxis ? ( 1 - ( double )i / ( rows - 1 ) ) : ( double )i / ( rows - 1 );

				for( int j = 0; j < columns; j++ )
				{
					var u = flipColumnsAxis ? ( 1 - ( double )j / ( columns - 1 ) ) : ( double )j / ( columns - 1 );
					this.m_textureCoordinates.Add( new Point( u, v ) );
				}
			}
		}

		public MeshGeometry3D ToMesh( bool freeze = false )
		{
			if( this.m_triangleIndices.Count == 0 )
			{
				var emptyGeometry = new MeshGeometry3D();
				if( freeze )
				{
					emptyGeometry.Freeze();
				}

				return emptyGeometry;
			}

			if( this.m_normals != null && this.m_positions.Count != this.m_normals.Count )
				throw new InvalidOperationException( "Cant't create mesh geometry. Mesh contains wrong number of normals." );

			if( this.m_textureCoordinates != null && this.m_positions.Count != this.m_textureCoordinates.Count )
				throw new InvalidOperationException( "Cant't create mesh geometry. Mesh contains wrong number of texture coordinates." );

			var mg = new MeshGeometry3D
			{
				Positions = new Point3DCollection( this.m_positions ),
				TriangleIndices = new Int32Collection( this.m_triangleIndices )
			};
			if( this.m_normals != null )
			{
				mg.Normals = new Vector3DCollection( this.m_normals );
			}

			if( this.m_textureCoordinates != null )
			{
				mg.TextureCoordinates = new PointCollection( this.m_textureCoordinates );
			}

			if( freeze )
			{
				mg.Freeze();
			}

			return mg;
		}

		[Flags]
		internal enum BoxSide
		{
			PositiveZ = 0x1,
			Top = PositiveZ,

			NegativeZ = 0x2,
			Bottom = NegativeZ,

			NegativeY = 0x4,
			Left = NegativeY,

			PositiveY = 0x8,
			Right = PositiveY,

			PositiveX = 0x10,
			Front = PositiveX,

			NegativeX = 0x20,
			Back = NegativeX,

			All = PositiveZ | NegativeZ | NegativeY | PositiveY | PositiveX | NegativeX
		}



		// override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources


		public void Dispose()
		{
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			if( m_isDisposed )
				return;

			if( disposing )
			{
				// dispose managed state (managed objects)

				if( m_circleCache != null )
				{
					//m_circleCache.Dispose();
					//m_circleCache = null;
				}

				if( m_closedCircleCache != null )
				{
					//m_closedCircleCache.Dispose();
					//m_closedCircleCache = null;
				}
			}

			// free unmanaged resources (unmanaged objects) and override finalizer

			// set large fields to null
			//m_normals = null;
			//m_positions = null;
			//m_textureCoordinates = null;
			//m_triangleIndices = null;

			m_isDisposed = true;
		}
	}
}
