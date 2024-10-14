using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D
{
	internal static class MaterialHelper
	{
		/// <summary>
		/// Создание материала с заданным рассеянным (diffuse) цветом и непрозрачностью.
		/// </summary>
		public static Material CreateMaterial( Color color, double opacity )
		{
			var colorWithOpacity = Color.FromArgb( ( byte )( opacity * 255 ), color.R, color.G, color.B );
			return CreateMaterial( colorWithOpacity );
		}

		/// <summary>
		/// Создание материала с заданным рассеянным (diffuse) цветом.
		/// </summary>
		public static Material CreateMaterial( Color color )
		{
			var brush = new SolidColorBrush( color );
			return CreateMaterial( brush );
		}

		public static Material CreateMaterial( Brush brush )
		{
			return CreateMaterial( brush, 1d );
		}

		public static Material CreateMaterial( Brush brush, double specularEffectBrightness )
		{
			return CreateMaterial( brush, specularEffectBrightness, 100, 255, true );
		}

		/// <summary>
		/// Создание материала с рассеянным (diffuse) цветом по заданной кисти.
		/// Можно добавить зеркальный эффект.
		/// </summary>
		public static Material CreateMaterial( Brush brush, double specularEffect = 100, byte ambient = 255, bool freeze = true )
		{
			return CreateMaterial( brush, 1d, specularEffect, ambient, freeze );
		}

		/// <summary>
		/// Создание материала с рассеянным (diffuse) цветом по заданной кисти.
		/// Можно добавить зеркальный эффект.
		/// </summary>
		/// <param name="brush">Кисть, в соответствии с которой создаетя материал.</param>
		/// <param name="specularEffectBrightness">Коэффициент зеркальности материала.</param>
		/// <param name="specularEffect">Параметр, отвечающий за эффект зеркального отражения материала.</param>
		/// <param name="ambientEffect">Параметр, отвечающий за влияние освещения AmbientLights на материал.</param>
		/// <param name="freeze">Замораживает материал при необходиммости (не может быть изменен в последствии).</param>
		public static Material CreateMaterial( Brush brush, double specularEffectBrightness, double specularEffect = 100, byte ambientEffect = 255, bool freeze = true )
		{
			var materialGroup = new MaterialGroup();
			materialGroup.Children.Add( new DiffuseMaterial( brush ) { AmbientColor = Color.FromRgb( ambientEffect, ambientEffect, ambientEffect ) } );

			// Задаем эффект зеркального отражения материала.
			if( specularEffect > 0 )
			{
				var b = ( byte )( 255 * specularEffectBrightness );
				materialGroup.Children.Add( new SpecularMaterial( new SolidColorBrush( Color.FromRgb( b, b, b ) ), specularEffect ) );
			}

			if( freeze )
			{
				// Делает текущий объект неизменяемым и устанавливает для его свойства System.Windows.Freezable.IsFrozen значение true.
				// Положительно влияет на производительность.
				materialGroup.Freeze();
			}

			return materialGroup;
		}
	}
}
