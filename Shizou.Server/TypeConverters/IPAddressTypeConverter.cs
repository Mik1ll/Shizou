using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

namespace Shizou.Server.TypeConverters;

public class IpAddressTypeConverter : TypeConverter
{
    public static readonly TypeConverterAttribute DefaultAttribute = new(typeof(IpAddressTypeConverter));

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string v ? IPAddress.Parse(v) : base.ConvertFrom(context, culture, value);

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string) && value is IPAddress v ? v.ToString() : base.ConvertTo(context, culture, value, destinationType);
}
