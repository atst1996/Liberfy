﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using Liberfy.Settings;
using Utf8Json;

namespace Liberfy.Components.JsonFormatters
{
    internal class AccountSettingsIEnumerableFormatter : UnionInterfaceEnumerableFormatterBase<IAccountSetting>
    {
        private readonly IReadOnlyDictionary<string, ServiceType> _serviceNameMap;

        public AccountSettingsIEnumerableFormatter()
        {
            var serviceTypeType = typeof(ServiceType);
            var fields = serviceTypeType.GetFields(BindingFlags.Static | BindingFlags.Public);
            var serviceNameMap = new Dictionary<string, ServiceType>(fields.Length);

            foreach (var field in fields)
            {
                var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
                serviceNameMap.Add(enumMember.Value, (ServiceType)field.GetValue(null));
            }

            this._serviceNameMap = serviceNameMap;
        }

        protected override IAccountSetting DeserializeItem(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            int offset = reader.GetCurrentOffsetUnsafe();

            // ServiceTypeを探す
            int count = 0;
            while (reader.ReadIsInObject(ref count))
            {
                var propertyName = reader.ReadPropertyName();
                if (propertyName == "service")
                {
                    var serviceTypeValue = reader.ReadString();

                    if (!this._serviceNameMap.TryGetValue(serviceTypeValue, out var serviceType))
                    {
                        throw new NotImplementedException();
                    }

                    reader.AdvanceOffset(offset - reader.GetCurrentOffsetUnsafe());

                    switch (serviceType)
                    {
                        case ServiceType.Twitter:
                            var twFormatter = formatterResolver.GetFormatter<TwitterAccountSetting>();
                            return twFormatter.Deserialize(ref reader, formatterResolver);

                        case ServiceType.Mastodon:
                            var mdFormatter = formatterResolver.GetFormatter<MastodonAccountSetting>();
                            return mdFormatter.Deserialize(ref reader, formatterResolver);

                        default:
                            throw new NotImplementedException();
                    }
                }

                Debug.WriteLine(propertyName);
                reader.ReadNextBlockSegment();
            }

            throw new NotImplementedException();
        }

        protected override void SerializeItem(ref JsonWriter writer, IAccountSetting value, IJsonFormatterResolver formatterResolver)
        {
            switch (value)
            {
                case TwitterAccountSetting twItem:
                    formatterResolver.GetFormatter<TwitterAccountSetting>().Serialize(ref writer, twItem, formatterResolver);
                    break;

                case MastodonAccountSetting mdItem:
                    formatterResolver.GetFormatter<MastodonAccountSetting>().Serialize(ref writer, mdItem, formatterResolver);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
