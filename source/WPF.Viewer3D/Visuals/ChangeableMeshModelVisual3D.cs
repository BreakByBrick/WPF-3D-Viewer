using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace WPF.Viewer3D.Visuals
{
	/// <summary>
	/// Изменяемая модель, состоящая из одной полигональной сетки.
	/// </summary>
	public abstract class ChangeableMeshModelVisual3D : ChangeableModelVisual3D
	{
		private GeometryModel3D m_model;

		private bool m_isMeshChanged = true;
		private bool m_isMaterialChanged = true;
		private bool m_isBackMaterialChanged = false;

		/// <summary>
		/// Видимость модели.
		/// </summary>
		public bool IsVisible
		{
			get => ( bool )this.GetValue( IsVisibleProperty );
			set => this.SetValue( IsVisibleProperty, value );
		}
		public static readonly DependencyProperty IsVisibleProperty;


		/// <summary>
		/// Материал фронтальной поверхности полигональной сетки модели.
		/// </summary>
		public Material Material
		{
			get => ( Material )this.GetValue( MaterialProperty );
			set => this.SetValue( MaterialProperty, value );
		}
		public static readonly DependencyProperty MaterialProperty;


		/// <summary>
		/// Материал обратной поверхности полигональной сетки модели.
		/// </summary>
		public Material BackMaterial
		{
			get => ( Material )this.GetValue( BackMaterialProperty );
			set => this.SetValue( BackMaterialProperty, value );
		}
		public static readonly DependencyProperty BackMaterialProperty;



		static ChangeableMeshModelVisual3D()
		{
			IsVisibleProperty = DependencyProperty.Register(
				"IsVisible",
				typeof( bool ),
				typeof( ChangeableMeshModelVisual3D ),
				new PropertyMetadata( true, VisibilityChangedCallback ) );

			MaterialProperty = DependencyProperty.Register(
				"Material",
				typeof( Material ),
				typeof( ChangeableMeshModelVisual3D ),
				new PropertyMetadata( MaterialHelper.CreateMaterial( Colors.Blue ), MaterialChangedCallback ) );

			BackMaterialProperty = DependencyProperty.Register(
				"BackMaterial",
				typeof( Material ),
				typeof( ChangeableMeshModelVisual3D ),
				new PropertyMetadata( MaterialHelper.CreateMaterial( Colors.LightBlue ), BackMaterialChangedCallback ) );
		}
		protected static void VisibilityChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			GeometryChangedCallback( d, e );
		}
		protected static void GeometryChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			ChangeableMeshModelVisual3D mesh = ( ChangeableMeshModelVisual3D )d;
			mesh.m_isMeshChanged = true;
			ChangedCallback( d, e );
		}
		protected static void MaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			ChangeableMeshModelVisual3D mesh = ( ChangeableMeshModelVisual3D )d;
			mesh.m_isMaterialChanged = true;
			ChangedCallback( d, e );
		}
		protected static void BackMaterialChangedCallback( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			ChangeableMeshModelVisual3D mesh = ( ChangeableMeshModelVisual3D )d;
			mesh.m_isBackMaterialChanged = true;
			ChangedCallback( d, e );
		}



		public ChangeableMeshModelVisual3D()
		{
			this.Content = m_model = new GeometryModel3D();
			UpdateModel();
		}



		protected override void UpdateModel()
		{
			UpdateMesh();
			UpdateMaterial();
			UpdateBackMaterial();
		}
		private void UpdateMesh()
		{
			if( !m_isMeshChanged )
				return;

			m_model.Geometry = this.IsVisible ? this.BuildMesh() : null;
			m_isMeshChanged = false;
		}

		/// <summary>
		/// Построение геометрии модели.
		/// </summary>
		protected abstract MeshGeometry3D BuildMesh();

		private void UpdateMaterial()
		{
			if( !m_isMaterialChanged )
				return;

			m_model.Material = Material;
			m_isMaterialChanged = false;
		}
		private void UpdateBackMaterial()
		{
			if( !m_isBackMaterialChanged )
				return;

			m_model.BackMaterial = BackMaterial;
			m_isBackMaterialChanged = false;
		}
	}
}
