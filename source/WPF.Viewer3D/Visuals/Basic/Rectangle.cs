using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	public class Rectangle : ChangeableMeshModelVisual3D
	{
        public int DivLength
        {
            get => ( int )this.GetValue( DivLengthProperty );
            set => this.SetValue( DivLengthProperty, value );
        }
        public static readonly DependencyProperty DivLengthProperty;

        public int DivWidth
        {
            get => ( int )this.GetValue( DivWidthProperty );
            set => this.SetValue( DivWidthProperty, value );
        }
        public static readonly DependencyProperty DivWidthProperty;

        public double Length
        {
            get => ( double )this.GetValue( LengthProperty );
            set => this.SetValue( LengthProperty, value );
        }
        public static readonly DependencyProperty LengthDirectionProperty;

        public Vector3D LengthDirection
        {
            get => ( Vector3D )this.GetValue( LengthDirectionProperty );
            set => this.SetValue( LengthDirectionProperty, value );
        }
        public static readonly DependencyProperty LengthProperty;

        public Vector3D Normal
        {
            get => ( Vector3D )this.GetValue( NormalProperty );
            set => this.SetValue( NormalProperty, value );
        }
        public static readonly DependencyProperty NormalProperty;

        public Point3D Origin
        {
            get => ( Point3D )this.GetValue( OriginProperty );
            set => this.SetValue( OriginProperty, value );
        }
        public static readonly DependencyProperty OriginProperty;

        public double Width
        {
            get => ( double )this.GetValue( WidthProperty );
            set => this.SetValue( WidthProperty, value );
        }
        public static readonly DependencyProperty WidthProperty;

        static Rectangle()
		{
            DivLengthProperty = DependencyProperty.Register(
                "DivLength", 
                typeof( int ), 
                typeof( Rectangle ), 
                new PropertyMetadata( 10, GeometryChangedCallback, CoerceDivValue ) );

            DivWidthProperty = DependencyProperty.Register(
                "DivWidth", 
                typeof( int ), 
                typeof( Rectangle ), 
                new PropertyMetadata( 10, GeometryChangedCallback, CoerceDivValue ) );

            LengthDirectionProperty =
            DependencyProperty.Register(
                "LengthDirection",
                typeof( Vector3D ),
                typeof( Rectangle ),
                new PropertyMetadata( new Vector3D( 1, 0, 0 ), GeometryChangedCallback ) );

            LengthProperty = DependencyProperty.Register(
                "Length", 
                typeof( double ), 
                typeof( Rectangle ), 
                new PropertyMetadata( 10.0, GeometryChangedCallback ) );

            NormalProperty = DependencyProperty.Register(
                "Normal",
                typeof( Vector3D ),
                typeof( Rectangle ),
                new PropertyMetadata( new Vector3D( 0, 0, 1 ), GeometryChangedCallback ) );

            OriginProperty = DependencyProperty.Register(
                "Origin",
                typeof( Point3D ),
                typeof( Rectangle ),
                new PropertyMetadata( new Point3D( 0, 0, 0 ), GeometryChangedCallback ) );

            WidthProperty = DependencyProperty.Register(
                "Width", 
                typeof( double ), 
                typeof( Rectangle ), 
                new PropertyMetadata( 10.0, GeometryChangedCallback ) );
        }

        private static object CoerceDivValue( DependencyObject d, object baseValue )
        {
            return Math.Max( 2, ( int )baseValue );
        }

        protected override MeshGeometry3D BuildMesh()
		{
            Vector3D u = this.LengthDirection;
            Vector3D w = this.Normal;
            Vector3D v = Vector3D.CrossProduct( w, u );
            u = Vector3D.CrossProduct( v, w );

            u.Normalize();
            v.Normalize();
            w.Normalize();

            double le = this.Length;
            double wi = this.Width;

            var pts = new List<Point3D>();
            for( int i = 0; i < this.DivLength; i++ )
            {
                double fi = -0.5 + ( ( double )i / ( this.DivLength - 1 ) );
                for( int j = 0; j < this.DivWidth; j++ )
                {
                    double fj = -0.5 + ( ( double )j / ( this.DivWidth - 1 ) );
                    pts.Add( this.Origin + ( u * le * fi ) + ( v * wi * fj ) );
                }
            }

            var builder = new MeshBuilder( false, true );
            builder.AddRectangularMesh( pts, this.DivWidth );

            return builder.ToMesh();
        }
	}
}
