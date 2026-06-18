using CefSharp.DevTools.LayerTree;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SLBr.Managers
{
    public class FavouriteManager
    {
        public readonly Dictionary<string, Favourite> UrlCache = [with(StringComparer.OrdinalIgnoreCase)];

        public ObservableCollection<Favourite> Favourites = [];
        public event EventHandler Changed;

        public FavouriteManager()
        {
            Favourites.CollectionChanged += OnFavouritesCollectionChanged;
        }

        private void OnFavouritesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                    RegisterEvents((Favourite)e.NewItems[i]!);
            }
            if (e.OldItems != null)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                    UnregisterEvents((Favourite)e.OldItems[i]!);
            }
            Changed?.Invoke(this, e);
        }

        private void RegisterEvents(Favourite Item)
        {
            if (!string.IsNullOrEmpty(Item.Url))
                UrlCache[Item.Url] = Item;
            //Item.PropertyChanged += OnFavouritePropertyChanged;
            if (Item.Children != null)
            {
                Item.Children.CollectionChanged -= OnFavouritesCollectionChanged;
                Item.Children.CollectionChanged += OnFavouritesCollectionChanged;
                for (int i = 0; i < Item.Children.Count; i++)
                    RegisterEvents(Item.Children[i]);
            }
        }

        private void UnregisterEvents(Favourite Item)
        {
            if (!string.IsNullOrEmpty(Item.Url))
                UrlCache.Remove(Item.Url);
            //Item.PropertyChanged -= OnFavouritePropertyChanged;
            if (Item.Children != null)
            {
                Item.Children.CollectionChanged -= OnFavouritesCollectionChanged;
                for (int i = 0; i < Item.Children.Count; i++)
                    UnregisterEvents(Item.Children[i]);
            }
        }

        public void Add(Favourite Item, Favourite? Parent)
        {
            if (Parent == null)
                Favourites.Add(Item);
            else
            {
                Item.Parent = Parent;
                Parent.Children ??= [];
                RegisterEvents(Parent);
                Parent.Children.Add(Item);
            }
            SetUp(Item);
        }

        public void Remove(Favourite Item)
        {
            if (Item.Parent == null)
                Favourites.Remove(Item);
            else
                Item.Parent.Children?.Remove(Item);
        }

        private void SetUp(Favourite? Item)
        {
            if (Item == null)
            {
                for (int i = 0; i < Favourites.Count; i++)
                    SetUp(Favourites[i]);
            }
            else
            {
                if (string.IsNullOrEmpty(Item.Name) && !string.IsNullOrEmpty(Item.Url))
                    Item.Name = Utils.FastHost(Item.Url);
                if (Item.Children != null)
                {
                    for (int i = 0; i < Item.Children.Count; i++)
                    {
                        Favourite Child = Item.Children[i];
                        Child.Parent = Item;
                        SetUp(Child);
                    }
                }
            }
        }

        public Favourite? Contains(string Url)
        {
            if (Favourites.Count == 0 || string.IsNullOrEmpty(Url))
                return null;
            return UrlCache.GetValueOrDefault(Url);
        }

        public (ObservableCollection<UIElementLayer>?, UIElementLayer?) GenerateFolderTreeLayers(Favourite? Parent = null)
        {
            ObservableCollection<UIElementLayer>? Layers = null;
            UIElementLayer? GeneratedParent = null;
            foreach (Favourite Item in Favourites)
            {
                if (Item.Type == "folder")
                {
                    Layers ??= [];
                    Layers.Add(ConvertToLayer(Item));
                }
            }
            return (Layers, GeneratedParent);
            UIElementLayer ConvertToLayer(Favourite Item)
            {
                UIElementLayer Layer = new()
                {
                    Text = Item.Name,
                    Icon = "\xe8b7",
                    Data = Item
                };

                if (Item.Children != null)
                {
                    foreach (Favourite Child in Item.Children)
                    {
                        if (string.IsNullOrEmpty(Child.Url))
                        {
                            Layer.Children ??= [];
                            Layer.Children.Add(ConvertToLayer(Child));
                        }
                    }
                }
                if (Item == Parent)
                    GeneratedParent = Layer;
                return Layer;
            }
        }
    }
}
