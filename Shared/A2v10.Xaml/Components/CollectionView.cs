﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace A2v10.Xaml
{
    [ContentProperty("Children")]
    public class CollectionView : Container
    {
        public Object ItemsSource { get; set; }

        //public RunMode RunAt { get; set; }

        internal override void RenderElement(RenderContext context)
        {
            var tag = new TagBuilder("collection-view");
            var itemsSource = GetBinding(nameof(ItemsSource));
            if (itemsSource != null)
                tag.MergeAttribute(":items-source", itemsSource.Path);

            tag.MergeAttribute("run-at", "server"); // TODO: run-at???
            tag.MergeAttribute(":page-size", "3"); // TODO: page-size???

            tag.RenderStart(context);
            var tml = new TagBuilder("template");
            tml.MergeAttribute("scope", "Parent");
            tml.RenderStart(context);
            RenderChildren(context);
            tml.RenderEnd(context);
            tag.RenderEnd(context);
        }
    }
}