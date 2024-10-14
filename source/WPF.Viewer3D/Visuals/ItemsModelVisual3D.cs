using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace WPF.Viewer3D.Visuals
{
	internal class ItemsModelVisual3D : ModelVisual3D
	{
		private readonly Dictionary<object, Visual3D> m_children = new Dictionary<object, Visual3D>();

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource",
			typeof( IEnumerable ),
			typeof( ItemsModelVisual3D ),
			new PropertyMetadata( null, ( s, e ) => ( ( ItemsModelVisual3D )s ).ItemsSourceChanged( e ) ) );

		public IEnumerable ItemsSource
		{
			get
			{
				return ( IEnumerable )this.GetValue( ItemsSourceProperty );
			}

			set
			{
				this.SetValue( ItemsSourceProperty, value );
			}
		}


		private void ItemsSourceChanged( DependencyPropertyChangedEventArgs e )
		{
			var oldObservableCollection = e.OldValue as INotifyCollectionChanged;
			if( oldObservableCollection != null )
			{
				oldObservableCollection.CollectionChanged -= this.CollectionChanged;
			}

			var observableCollection = e.NewValue as INotifyCollectionChanged;
			if( observableCollection != null )
			{
				observableCollection.CollectionChanged += this.CollectionChanged;
			}

			if( this.ItemsSource != null )
			{
				AddItems( this.ItemsSource );
			}

			RefreshChildren();
		}
		private void CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			switch( e.Action )
			{
				case NotifyCollectionChangedAction.Add:
					AddItems( e.NewItems );
					break;

				case NotifyCollectionChangedAction.Remove:
					RemoveItems( e.OldItems );
					break;

				case NotifyCollectionChangedAction.Replace:
					RemoveItems( e.OldItems );
					AddItems( e.NewItems );
					break;

				case NotifyCollectionChangedAction.Reset:
					this.Children.Clear();
					this.m_children.Clear();

					this.AddItems( ItemsSource );
					break;

				default:
					break;
			}

			RefreshChildren();
		}
		private void AddItems( IEnumerable items )
		{
			if( items == null )
				return;

			foreach( var item in items )
				AddItem( item );
		}
		private void AddItem( object item )
		{
			var visual = item as Visual3D;
			if( visual == null )
				throw new InvalidOperationException( "Can't add item. Item is not a Visual3D." );

			this.Children.Add( visual );
			this.m_children[ item ] = visual;
		}
		private void RemoveItems( IEnumerable items )
		{
			if( items == null )
				return;

			foreach( var item in items )
			{
				if( m_children.ContainsKey( item ) )
				{
					var child = m_children[ item ];
					if( child != null )
					{
						Children.Remove( child );
					}
				}
			}
		}


		public void RefreshChildren()
		{
			var viewPort = this.GetViewport3D();
			var index = viewPort.Children.IndexOf( this );
			viewPort.Children.Remove( this );
			viewPort.Children.Insert( index, this );
		}
	}
}
