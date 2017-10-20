﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Markup;

using A2v10.Infrastructure;
using System.ComponentModel;
using System.Globalization;

namespace A2v10.Xaml
{

    public class FilterItem
    {
        public String Property { get; set; }
        public Object Value { get; set; }
        public DataType DataType { get; set; }

        internal String GetJsValue()
        {
            switch (DataType)
            {
                case DataType.String:
                    return Value == null ? "''" : $"'{Value}'";
                case DataType.Number:
                case DataType.Currency:
                    return Value == null ? "0" : $"+{Value}";
                case DataType.Boolean:
                    if (Value == null)
                        return "false";
                    if (Value is String)
                    {
                        var strVal = Value.ToString();
                        if (strVal == "True" || strVal == "False")
                            return strVal.ToLowerInvariant();
                    }
                    throw new NotImplementedException($"Invalid value for Boolean FilterItem '{Value}'");
                default:
                    throw new NotImplementedException("type for FilterItem");
            }
        }
    }

    public class FilterItems : List<FilterItem>
    {

    }

    [ContentProperty("Items")]
    [TypeConverter(typeof(FilterDescriptionConverter))]
    public class FilterDescription : IJavaScriptSource
    {
        public FilterDescription()
        {

        }

        public FilterDescription(String from)
        {
            foreach (var x in from.Split(','))
            {
                var item = new FilterItem();
                item.DataType = DataType.String;
                item.Property = x.Trim();
                Items.Add(item);
            }
        }

        public FilterItems Items { get; set; } = new FilterItems();

        public String GetJsValue()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            foreach (var itm in Items)
            {
                sb.Append($"{itm.Property.EncodeJs()}: {itm.GetJsValue()}, ");
            }
            sb.RemoveTailComma();
            sb.Append("}");
            return sb.ToString();
        }
    }

    internal class FilterDescriptionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(String))
                return true;
            else if (sourceType == typeof(FilterDescription))
                return true;
            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;
            if (value is String)
                return new FilterDescription(value.ToString());
            else if (value is FilterDescription)
                return value;
            throw new XamlException($"Invalid FilterDescription value '{value}'");
        }
    }
}