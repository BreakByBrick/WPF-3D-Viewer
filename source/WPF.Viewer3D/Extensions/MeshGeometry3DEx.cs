using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using WPF.Viewer3D.Visuals;

namespace WPF.Viewer3D
{
	internal static class MeshGeometry3DEx
	{
		/// <summary>
		/// Добавляет сечение в соответствии с заданной плоскостью.
		/// </summary>
		public static MeshGeometry3D Cut( this MeshGeometry3D mesh, Point3D plane, Vector3D normal )
		{
			var hasTextureCoordinates = mesh.TextureCoordinates != null && mesh.TextureCoordinates.Count > 0;
			var hasNormals = mesh.Normals != null && mesh.Normals.Count > 0;
			var meshBuilder = new MeshBuilder( hasNormals, hasTextureCoordinates );
			var contourFacet = new ContourFacet( plane, normal, mesh );
			foreach( var position in mesh.Positions )
			{
				meshBuilder.Positions.Add( position );
			}

			if( hasTextureCoordinates )
			{
				foreach( var textureCoordinate in mesh.TextureCoordinates )
				{
					meshBuilder.TextureCoordinates.Add( textureCoordinate );
				}
			}

			if( hasNormals )
			{
				foreach( var n in mesh.Normals )
				{
					meshBuilder.Normals.Add( n );
				}
			}

			for( var i = 0; i < mesh.TriangleIndices.Count; i += 3 )
			{
				var index0 = mesh.TriangleIndices[ i ];
				var index1 = mesh.TriangleIndices[ i + 1 ];
				var index2 = mesh.TriangleIndices[ i + 2 ];

				Point3D[] positions;
				Vector3D[] normals;
				Point[] textureCoordinates;
				int[] triangleIndices;

				contourFacet.Apply( index0, index1, index2, out positions, out normals, out textureCoordinates, out triangleIndices );

				foreach( var p in positions )
				{
					meshBuilder.Positions.Add( p );
				}

				foreach( var tc in textureCoordinates )
				{
					meshBuilder.TextureCoordinates.Add( tc );
				}

				foreach( var n in normals )
				{
					meshBuilder.Normals.Add( n );
				}

				foreach( var ti in triangleIndices )
				{
					meshBuilder.TriangleIndices.Add( ti );
				}
			}

			return meshBuilder.ToMesh();
		}
	}
	internal class ContourFacet
	{
		/// <summary>
		/// Наборы индексов для различных случаев.
		/// </summary>
		private static readonly IDictionary<ContourFacetResult, int[,]> m_resultIndices
			= new Dictionary<ContourFacetResult, int[,]>
		{
			{ ContourFacetResult.ZeroOnly, new[,] { { 0, 1 }, { 0, 2 } } },
			{ ContourFacetResult.OneAndTwo, new[,] { { 0, 2 }, { 0, 1 } } },
			{ ContourFacetResult.OneOnly, new[,] { { 1, 2 }, { 1, 0 } } },
			{ ContourFacetResult.ZeroAndTwo, new[,] { { 1, 0 }, { 1, 2 } } },
			{ ContourFacetResult.TwoOnly, new[,] { { 2, 0 }, { 2, 1 } } },
			{ ContourFacetResult.ZeroAndOne, new[,] { { 2, 1 }, { 2, 0 } } },
		};

		private readonly double m_a;
		private readonly double m_b;
		private readonly double m_c;
		private readonly double m_d;

		private readonly double[] m_sides = new double[ 3 ];
		private readonly int[] m_indices = new int[ 3 ];

		private readonly Point3D[] m_originalPositions;
		private readonly Vector3D[] m_originalNormals;
		private readonly Point[] m_originalhTextureCoordinates;

		private readonly Point3D[] m_newPositions = new Point3D[ 3 ];
		private readonly Vector3D[] m_newNormals;
		private readonly Point[] m_newTextureCoordinates;

		private int m_positionCount;

		public ContourFacet( Point3D planeOrigin, Vector3D planeNormal, MeshGeometry3D originalMesh )
		{
			var hasNormals = originalMesh.Normals != null && originalMesh.Normals.Count > 0;
			var hasTextureCoordinates = originalMesh.TextureCoordinates != null && originalMesh.TextureCoordinates.Count > 0;
			this.m_newNormals = hasNormals ? new Vector3D[ 3 ] : null;
			this.m_newTextureCoordinates = hasTextureCoordinates ? new Point[ 3 ] : null;
			this.m_positionCount = originalMesh.Positions.Count;

			this.m_originalPositions = originalMesh.Positions.ToArray();
			this.m_originalNormals = hasNormals ? originalMesh.Normals.ToArray() : null;
			this.m_originalhTextureCoordinates = hasTextureCoordinates ? originalMesh.TextureCoordinates.ToArray() : null;

			// Определение уравнения плоскости:
			// ax + by + cz + d = 0
			var l = ( float )Math.Sqrt( ( planeNormal.X * planeNormal.X ) + ( planeNormal.Y * planeNormal.Y ) + ( planeNormal.Z * planeNormal.Z ) );
			this.m_a = planeNormal.X / l;
			this.m_b = planeNormal.Y / l;
			this.m_c = planeNormal.Z / l;
			this.m_d = -( float )( ( planeNormal.X * planeOrigin.X ) + ( planeNormal.Y * planeOrigin.Y ) + ( planeNormal.Z * planeOrigin.Z ) );
		}


		/// <summary>
		/// Создает контурный срез через фасет с 3 вершинами.
		/// </summary>
		public void Apply(
			int index0,
			int index1,
			int index2,
			out Point3D[] newPositions,
			out Vector3D[] newNormals,
			out Point[] newTextureCoordinates,
			out int[] triangleIndices )
		{
			this.SetData( index0, index1, index2 );

			var facetResult = this.GetContourFacet();

			switch( facetResult )
			{
				case ContourFacetResult.ZeroOnly:
					triangleIndices = new[] { index0, this.m_positionCount++, this.m_positionCount++ };
					break;
				case ContourFacetResult.OneAndTwo:
					triangleIndices = new[] { index1, index2, this.m_positionCount, this.m_positionCount++, this.m_positionCount++, index1 };
					break;
				case ContourFacetResult.OneOnly:
					triangleIndices = new[] { index1, this.m_positionCount++, this.m_positionCount++ };
					break;
				case ContourFacetResult.ZeroAndTwo:
					triangleIndices = new[] { index2, index0, this.m_positionCount, this.m_positionCount++, this.m_positionCount++, index2 };
					break;
				case ContourFacetResult.TwoOnly:
					triangleIndices = new[] { index2, this.m_positionCount++, this.m_positionCount++ };
					break;
				case ContourFacetResult.ZeroAndOne:
					triangleIndices = new[] { index0, index1, this.m_positionCount, this.m_positionCount++, this.m_positionCount++, index0 };
					break;
				case ContourFacetResult.All:
					newPositions = new Point3D[ 0 ];
					newNormals = new Vector3D[ 0 ];
					newTextureCoordinates = new Point[ 0 ];
					triangleIndices = new[] { index0, index1, index2 };
					return;
				default:
					newPositions = new Point3D[ 0 ];
					newNormals = new Vector3D[ 0 ];
					newTextureCoordinates = new Point[ 0 ];
					triangleIndices = new int[ 0 ];
					return;
			}

			var facetIndices = m_resultIndices[ facetResult ];
			newPositions = new[]
			{
				this.CreateNewPosition(facetIndices[0, 0], facetIndices[0, 1]),
				this.CreateNewPosition(facetIndices[1, 0], facetIndices[1, 1])
			};

			if( this.m_newNormals != null )
			{
				newNormals = new[]
			{
				this.CreateNewNormal(facetIndices[0, 0], facetIndices[0, 1]),
				this.CreateNewNormal(facetIndices[1, 0], facetIndices[1, 1])
			};
			}
			else
			{
				newNormals = new Vector3D[ 0 ];
			}

			if( this.m_newTextureCoordinates != null )
			{
				newTextureCoordinates = new[]
				{
					this.CreateNewTexture(facetIndices[0, 0], facetIndices[0, 1]),
					this.CreateNewTexture(facetIndices[1, 0], facetIndices[1, 1])
				};
			}
			else
			{
				newTextureCoordinates = new Point[ 0 ];
			}
		}
		private void SetData( int index0, int index1, int index2 )
		{
			this.m_indices[ 0 ] = index0;
			this.m_indices[ 1 ] = index1;
			this.m_indices[ 2 ] = index2;

			this.m_newPositions[ 0 ] = this.m_originalPositions[ index0 ];
			this.m_newPositions[ 1 ] = this.m_originalPositions[ index1 ];
			this.m_newPositions[ 2 ] = this.m_originalPositions[ index2 ];

			if( this.m_newNormals != null )
			{
				this.m_newNormals[ 0 ] = this.m_originalNormals[ index0 ];
				this.m_newNormals[ 1 ] = this.m_originalNormals[ index1 ];
				this.m_newNormals[ 2 ] = this.m_originalNormals[ index2 ];
			}

			if( this.m_newTextureCoordinates != null )
			{
				this.m_newTextureCoordinates[ 0 ] = this.m_originalhTextureCoordinates[ index0 ];
				this.m_newTextureCoordinates[ 1 ] = this.m_originalhTextureCoordinates[ index1 ];
				this.m_newTextureCoordinates[ 2 ] = this.m_originalhTextureCoordinates[ index2 ];
			}

			this.m_sides[ 0 ] = ( this.m_a * this.m_newPositions[ 0 ].X ) + ( this.m_b * this.m_newPositions[ 0 ].Y ) + ( this.m_c * this.m_newPositions[ 0 ].Z ) + this.m_d;
			this.m_sides[ 1 ] = ( this.m_a * this.m_newPositions[ 1 ].X ) + ( this.m_b * this.m_newPositions[ 1 ].Y ) + ( this.m_c * this.m_newPositions[ 1 ].Z ) + this.m_d;
			this.m_sides[ 2 ] = ( this.m_a * this.m_newPositions[ 2 ].X ) + ( this.m_b * this.m_newPositions[ 2 ].Y ) + ( this.m_c * this.m_newPositions[ 2 ].Z ) + this.m_d;
		}
		private ContourFacetResult GetContourFacet()
		{
			if( this.IsSideAlone( 0 ) )
			{
				return this.m_sides[ 0 ] > 0 ? ContourFacetResult.ZeroOnly : ContourFacetResult.OneAndTwo;
			}

			if( this.IsSideAlone( 1 ) )
			{
				return this.m_sides[ 1 ] > 0 ? ContourFacetResult.OneOnly : ContourFacetResult.ZeroAndTwo;
			}

			if( this.IsSideAlone( 2 ) )
			{
				return this.m_sides[ 2 ] > 0 ? ContourFacetResult.TwoOnly : ContourFacetResult.ZeroAndOne;
			}

			if( this.IsAllSidesBelowContour() )
			{
				return ContourFacetResult.All;
			}

			return ContourFacetResult.None;
		}
		/// <summary>
		/// Определяет, находится ли вершина указанного индекса на противоположной стороне от двух других вершин.
		/// </summary>
		private bool IsSideAlone( int index )
		{
			Func<int, int> getNext = i => i + 1 > 2 ? 0 : i + 1;

			var firstSideIndex = getNext( index );
			var secondSideIndex = getNext( firstSideIndex );
			return this.m_sides[ index ] * this.m_sides[ firstSideIndex ] < 0
				&& this.m_sides[ index ] * this.m_sides[ secondSideIndex ] < 0;
		}
		/// <summary>
		/// Определяет, все ли стороны грани находятся ниже контура.
		/// </summary>
		private bool IsAllSidesBelowContour()
		{
			return this.m_sides[ 0 ] >= 0
				&& this.m_sides[ 1 ] >= 0
				&& this.m_sides[ 2 ] >= 0;
		}



		/// <summary>
		/// Вычисляет положение на пересечении плоскостей для стороны, указанной двумя индексами треугольника.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Point3D CreateNewPosition( int index0, int index1 )
		{
			var firstPoint = this.m_newPositions[ index0 ];
			var secondPoint = this.m_newPositions[ index1 ];
			var firstSide = this.m_sides[ index0 ];
			var secondSide = this.m_sides[ index1 ];
			return new Point3D(
				CalculatePoint( firstPoint.X, secondPoint.X, firstSide, secondSide ),
				CalculatePoint( firstPoint.Y, secondPoint.Y, firstSide, secondSide ),
				CalculatePoint( firstPoint.Z, secondPoint.Z, firstSide, secondSide ) );
		}
		/// <summary>
		/// Вычисляет нормаль в пересечении плоскостей для стороны, указанной двумя индексами треугольника.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Vector3D CreateNewNormal( int index0, int index1 )
		{
			var firstPoint = this.m_newNormals[ index0 ];
			var secondPoint = this.m_newNormals[ index1 ];
			var firstSide = this.m_sides[ index0 ];
			var secondSide = this.m_sides[ index1 ];
			return new Vector3D(
				CalculatePoint( firstPoint.X, secondPoint.X, firstSide, secondSide ),
				CalculatePoint( firstPoint.Y, secondPoint.Y, firstSide, secondSide ),
				CalculatePoint( firstPoint.Z, secondPoint.Z, firstSide, secondSide ) );
		}
		/// <summary>
		/// Вычисляет координату текстуры на пересечении плоскостей для стороны, указанной двумя индексами треугольника.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Point CreateNewTexture( int index0, int index1 )
		{
			var firstTexture = this.m_newTextureCoordinates[ index0 ];
			var secondTexture = this.m_newTextureCoordinates[ index1 ];
			var firstSide = this.m_sides[ index0 ];
			var secondSide = this.m_sides[ index1 ];

			return new Point(
				CalculatePoint( firstTexture.X, secondTexture.X, firstSide, secondSide ),
				CalculatePoint( firstTexture.Y, secondTexture.Y, firstSide, secondSide ) );
		}
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static double CalculatePoint( double firstPoint, double secondPoint, double firstSide, double secondSide )
		{
			return firstPoint - ( firstSide * ( secondPoint - firstPoint ) / ( secondSide - firstSide ) );
		}



		private enum ContourFacetResult
		{
			/// <summary>
			/// Все точки лежат над плоскостью контура.
			/// </summary>
			None,

			/// <summary>
			/// Только 0-я точка опускается ниже контурной плоскости.
			/// </summary>
			ZeroOnly,

			/// <summary>
			/// 1-я и 2-я точки попадают ниже контурной плоскости.
			/// </summary>
			OneAndTwo,

			/// <summary>
			/// Только 1-я точка опускается ниже контурной плоскости.
			/// </summary>
			OneOnly,

			/// <summary>
			/// 0-я и 2-я точки попадают ниже контурной плоскости.
			/// </summary>
			ZeroAndTwo,

			/// <summary>
			/// Только вторая точка опускается ниже плоскости контура.
			/// </summary>
			TwoOnly,

			/// <summary>
			/// 0-я и 1-я точки попадают ниже контурной плоскости.
			/// </summary>
			ZeroAndOne,

			/// <summary>
			/// Все точки попадают ниже контурной плоскости.
			/// </summary>
			All
		}
	}
}
