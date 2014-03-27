/**
 * Autogenerated by Thrift Compiler (0.9.0)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace Ably.Protocol
{

#if !SILVERLIGHT
    [Serializable]
#endif
    public partial class TError : TBase
    {
        private short _statusCode;
        private short _code;
        private string _reason;

        public short StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                __isset.statusCode = true;
                this._statusCode = value;
            }
        }

        public short Code
        {
            get
            {
                return _code;
            }
            set
            {
                __isset.code = true;
                this._code = value;
            }
        }

        public string Reason
        {
            get
            {
                return _reason;
            }
            set
            {
                __isset.reason = true;
                this._reason = value;
            }
        }


        public Isset __isset;
#if !SILVERLIGHT
        [Serializable]
#endif
        public struct Isset
        {
            public bool statusCode;
            public bool code;
            public bool reason;
        }

        public TError()
        {
        }

        public void Read(TProtocol iprot)
        {
            TField field;
            iprot.ReadStructBegin();
            while (true)
            {
                field = iprot.ReadFieldBegin();
                if (field.Type == Thrift.Protocol.TType.Stop)
                {
                    break;
                }
                switch (field.ID)
                {
                    case 1:
                        if (field.Type == Thrift.Protocol.TType.I16)
                        {
                            StatusCode = iprot.ReadI16();
                        }
                        else
                        {
                            TProtocolUtil.Skip(iprot, field.Type);
                        }
                        break;
                    case 2:
                        if (field.Type == Thrift.Protocol.TType.I16)
                        {
                            Code = iprot.ReadI16();
                        }
                        else
                        {
                            TProtocolUtil.Skip(iprot, field.Type);
                        }
                        break;
                    case 3:
                        if (field.Type == Thrift.Protocol.TType.String)
                        {
                            Reason = iprot.ReadString();
                        }
                        else
                        {
                            TProtocolUtil.Skip(iprot, field.Type);
                        }
                        break;
                    default:
                        TProtocolUtil.Skip(iprot, field.Type);
                        break;
                }
                iprot.ReadFieldEnd();
            }
            iprot.ReadStructEnd();
        }

        public void Write(TProtocol oprot)
        {
            TStruct struc = new TStruct("TError");
            oprot.WriteStructBegin(struc);
            TField field = new TField();
            if (__isset.statusCode)
            {
                field.Name = "statusCode";
                field.Type = Thrift.Protocol.TType.I16;
                field.ID = 1;
                oprot.WriteFieldBegin(field);
                oprot.WriteI16(StatusCode);
                oprot.WriteFieldEnd();
            }
            if (__isset.code)
            {
                field.Name = "code";
                field.Type = Thrift.Protocol.TType.I16;
                field.ID = 2;
                oprot.WriteFieldBegin(field);
                oprot.WriteI16(Code);
                oprot.WriteFieldEnd();
            }
            if (Reason != null && __isset.reason)
            {
                field.Name = "reason";
                field.Type = Thrift.Protocol.TType.String;
                field.ID = 3;
                oprot.WriteFieldBegin(field);
                oprot.WriteString(Reason);
                oprot.WriteFieldEnd();
            }
            oprot.WriteFieldStop();
            oprot.WriteStructEnd();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("TError(");
            sb.Append("StatusCode: ");
            sb.Append(StatusCode);
            sb.Append(",Code: ");
            sb.Append(Code);
            sb.Append(",Reason: ");
            sb.Append(Reason);
            sb.Append(")");
            return sb.ToString();
        }

    }

}
