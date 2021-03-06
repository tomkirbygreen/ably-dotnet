﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IO.Ably.CustomSerialisers {

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class IO_Ably_MessageCountSerializer : MsgPack.Serialization.MessagePackSerializer<IO.Ably.MessageCount> {
        
        private MsgPack.Serialization.MessagePackSerializer<string> _serializer0;
        
        private MsgPack.Serialization.MessagePackSerializer<long> _serializer1;
        
        public IO_Ably_MessageCountSerializer(MsgPack.Serialization.SerializationContext context) : 
                base(context) {
            MsgPack.Serialization.PolymorphismSchema schema0 = default(MsgPack.Serialization.PolymorphismSchema);
            schema0 = null;
            this._serializer0 = context.GetSerializer<string>(schema0);
            MsgPack.Serialization.PolymorphismSchema schema1 = default(MsgPack.Serialization.PolymorphismSchema);
            schema1 = null;
            this._serializer1 = context.GetSerializer<long>(schema1);
        }
        
        protected override void PackToCore(MsgPack.Packer packer, IO.Ably.MessageCount objectTree) {
            packer.PackMapHeader(2);
            this._serializer0.PackTo(packer, "count");
            this._serializer1.PackTo(packer, objectTree.Count);
            this._serializer0.PackTo(packer, "data");
            this._serializer1.PackTo(packer, objectTree.Data);
        }
        
        protected override IO.Ably.MessageCount UnpackFromCore(MsgPack.Unpacker unpacker)
        {
            IO.Ably.MessageCount result = default(IO.Ably.MessageCount);
            result = new IO.Ably.MessageCount();
            int itemsCount0 = default(int);
            itemsCount0 = MsgPack.Serialization.UnpackHelpers.GetItemsCount(unpacker);
            for (int i = 0; (i < itemsCount0); i = (i + 1))
            {
                string key = default(string);
                string nullable1 = default(string);
                nullable1 = MsgPack.Serialization.UnpackHelpers.UnpackStringValue(unpacker, typeof(IO.Ably.MessageCount),
                    "MemberName");
                if (((nullable1 == null)
                     == false))
                {
                    key = nullable1;
                }
                else
                {
                    throw MsgPack.Serialization.SerializationExceptions.NewNullIsProhibited("MemberName");
                }
                if ((key == "data"))
                {
                    System.Nullable<long> nullable3 = default(System.Nullable<long>);
                    nullable3 = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt64Value(unpacker,
                        typeof(IO.Ably.MessageCount), "Double Data");
                    if (nullable3.HasValue)
                    {
                        result.Data = nullable3.Value;
                    }
                }
                else
                {
                    if ((key == "count"))
                    {
                        System.Nullable<long> nullable2 = default(System.Nullable<long>);
                        nullable2 = MsgPack.Serialization.UnpackHelpers.UnpackNullableInt64Value(unpacker,
                            typeof(IO.Ably.MessageCount), "Double Count");
                        if (nullable2.HasValue)
                        {
                            result.Count = nullable2.Value;
                        }
                    }
                    else
                    {
                        unpacker.Skip();
                    }
                }
            }
            return result;
        }

        private static T @__Conditional<T>(bool condition, T whenTrue, T whenFalse)
         {
            if (condition) {
                return whenTrue;
            }
            else {
                return whenFalse;
            }
        }
    }
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
