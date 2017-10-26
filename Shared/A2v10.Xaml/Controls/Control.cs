﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A2v10.Xaml
{
	public abstract class Control : UIElement
	{
        public Boolean Block { get; set; }

        public String Label { get; set; }

        public String Description { get; set; }

        public Boolean Required { get; set; }

        public Boolean Disabled { get; set; }

        public Int32 TabIndex { get; set; }

        Lazy<UIElementCollection> _addOns = new Lazy<UIElementCollection>();

        public UIElementCollection AddOns { get { return _addOns.Value;} }

        internal override void MergeAttributes(TagBuilder tag, RenderContext context, MergeAttrMode mode = MergeAttrMode.All)
        {
            base.MergeAttributes(tag, context, mode);
            if (Block)
                tag.AddCssClass("block");
            AddControlAttributes(tag, context);
        }

        private void AddControlAttributes(TagBuilder tag, RenderContext context)
        {
            MergeBindingAttributeString(tag, context, "label", nameof(Label), Label);
            MergeBindingAttributeString(tag, context, "description", nameof(Description), Description);
            MergeBoolAttribute(tag, context, nameof(Required), Required);
            if (TabIndex != 0)
                tag.MergeAttribute(":tab-index", TabIndex.ToString());
        }

        internal void RenderAddOns(RenderContext context)
        {
            if (!_addOns.IsValueCreated)
                return;
            foreach (var ctl in AddOns)
            {
                ctl.RenderElement(context, (tag) =>
                {
                    tag.AddCssClass("add-on");
                    tag.MergeAttribute("tabindex", "-1");
                    MergeDisabled(tag, context);
                });
            }
        }

        internal virtual void MergeDisabled(TagBuilder tag, RenderContext context)
        {
            var disBind = GetBinding(nameof(Disabled));
            if (disBind != null)
                tag.MergeAttribute(":disabled", disBind.GetPath(context));
            else if (Disabled)
                tag.MergeAttribute("disabled", String.Empty);
        }
    }
}
