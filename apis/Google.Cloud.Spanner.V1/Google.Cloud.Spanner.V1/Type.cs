// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: google/spanner/v1/type.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Google.Cloud.Spanner.V1 {

  /// <summary>Holder for reflection information generated from google/spanner/v1/type.proto</summary>
  public static partial class TypeReflection {

    #region Descriptor
    /// <summary>File descriptor for google/spanner/v1/type.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TypeReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Chxnb29nbGUvc3Bhbm5lci92MS90eXBlLnByb3RvEhFnb29nbGUuc3Bhbm5l",
            "ci52MRocZ29vZ2xlL2FwaS9hbm5vdGF0aW9ucy5wcm90byKaAQoEVHlwZRIp",
            "CgRjb2RlGAEgASgOMhsuZ29vZ2xlLnNwYW5uZXIudjEuVHlwZUNvZGUSMwoS",
            "YXJyYXlfZWxlbWVudF90eXBlGAIgASgLMhcuZ29vZ2xlLnNwYW5uZXIudjEu",
            "VHlwZRIyCgtzdHJ1Y3RfdHlwZRgDIAEoCzIdLmdvb2dsZS5zcGFubmVyLnYx",
            "LlN0cnVjdFR5cGUifwoKU3RydWN0VHlwZRIzCgZmaWVsZHMYASADKAsyIy5n",
            "b29nbGUuc3Bhbm5lci52MS5TdHJ1Y3RUeXBlLkZpZWxkGjwKBUZpZWxkEgwK",
            "BG5hbWUYASABKAkSJQoEdHlwZRgCIAEoCzIXLmdvb2dsZS5zcGFubmVyLnYx",
            "LlR5cGUqjgEKCFR5cGVDb2RlEhkKFVRZUEVfQ09ERV9VTlNQRUNJRklFRBAA",
            "EggKBEJPT0wQARIJCgVJTlQ2NBACEgsKB0ZMT0FUNjQQAxINCglUSU1FU1RB",
            "TVAQBBIICgREQVRFEAUSCgoGU1RSSU5HEAYSCQoFQllURVMQBxIJCgVBUlJB",
            "WRAIEgoKBlNUUlVDVBAJQpIBChVjb20uZ29vZ2xlLnNwYW5uZXIudjFCCVR5",
            "cGVQcm90b1ABWjhnb29nbGUuZ29sYW5nLm9yZy9nZW5wcm90by9nb29nbGVh",
            "cGlzL3NwYW5uZXIvdjE7c3Bhbm5lcqoCF0dvb2dsZS5DbG91ZC5TcGFubmVy",
            "LlYxygIXR29vZ2xlXENsb3VkXFNwYW5uZXJcVjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Api.AnnotationsReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Google.Cloud.Spanner.V1.TypeCode), }, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Google.Cloud.Spanner.V1.Type), global::Google.Cloud.Spanner.V1.Type.Parser, new[]{ "Code", "ArrayElementType", "StructType" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Google.Cloud.Spanner.V1.StructType), global::Google.Cloud.Spanner.V1.StructType.Parser, new[]{ "Fields" }, null, null, new pbr::GeneratedClrTypeInfo[] { new pbr::GeneratedClrTypeInfo(typeof(global::Google.Cloud.Spanner.V1.StructType.Types.Field), global::Google.Cloud.Spanner.V1.StructType.Types.Field.Parser, new[]{ "Name", "Type" }, null, null, null)})
          }));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// `TypeCode` is used as part of [Type][google.spanner.v1.Type] to
  /// indicate the type of a Cloud Spanner value.
  ///
  /// Each legal value of a type can be encoded to or decoded from a JSON
  /// value, using the encodings described below. All Cloud Spanner values can
  /// be `null`, regardless of type; `null`s are always encoded as a JSON
  /// `null`.
  /// </summary>
  public enum TypeCode {
    /// <summary>
    /// Not specified.
    /// </summary>
    [pbr::OriginalName("TYPE_CODE_UNSPECIFIED")] Unspecified = 0,
    /// <summary>
    /// Encoded as JSON `true` or `false`.
    /// </summary>
    [pbr::OriginalName("BOOL")] Bool = 1,
    /// <summary>
    /// Encoded as `string`, in decimal format.
    /// </summary>
    [pbr::OriginalName("INT64")] Int64 = 2,
    /// <summary>
    /// Encoded as `number`, or the strings `"NaN"`, `"Infinity"`, or
    /// `"-Infinity"`.
    /// </summary>
    [pbr::OriginalName("FLOAT64")] Float64 = 3,
    /// <summary>
    /// Encoded as `string` in RFC 3339 timestamp format. The time zone
    /// must be present, and must be `"Z"`.
    ///
    /// If the schema has the column option
    /// `allow_commit_timestamp=true`, the placeholder string
    /// `"spanner.commit_timestamp()"` can be used to instruct the system
    /// to insert the commit timestamp associated with the transaction
    /// commit.
    /// </summary>
    [pbr::OriginalName("TIMESTAMP")] Timestamp = 4,
    /// <summary>
    /// Encoded as `string` in RFC 3339 date format.
    /// </summary>
    [pbr::OriginalName("DATE")] Date = 5,
    /// <summary>
    /// Encoded as `string`.
    /// </summary>
    [pbr::OriginalName("STRING")] String = 6,
    /// <summary>
    /// Encoded as a base64-encoded `string`, as described in RFC 4648,
    /// section 4.
    /// </summary>
    [pbr::OriginalName("BYTES")] Bytes = 7,
    /// <summary>
    /// Encoded as `list`, where the list elements are represented
    /// according to
    /// [array_element_type][google.spanner.v1.Type.array_element_type].
    /// </summary>
    [pbr::OriginalName("ARRAY")] Array = 8,
    /// <summary>
    /// Encoded as `list`, where list element `i` is represented according
    /// to [struct_type.fields[i]][google.spanner.v1.StructType.fields].
    /// </summary>
    [pbr::OriginalName("STRUCT")] Struct = 9,
  }

  #endregion

  #region Messages
  /// <summary>
  /// `Type` indicates the type of a Cloud Spanner value, as might be stored in a
  /// table cell or returned from an SQL query.
  /// </summary>
  public sealed partial class Type : pb::IMessage<Type> {
    private static readonly pb::MessageParser<Type> _parser = new pb::MessageParser<Type>(() => new Type());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<Type> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Google.Cloud.Spanner.V1.TypeReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Type() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Type(Type other) : this() {
      code_ = other.code_;
      arrayElementType_ = other.arrayElementType_ != null ? other.arrayElementType_.Clone() : null;
      structType_ = other.structType_ != null ? other.structType_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Type Clone() {
      return new Type(this);
    }

    /// <summary>Field number for the "code" field.</summary>
    public const int CodeFieldNumber = 1;
    private global::Google.Cloud.Spanner.V1.TypeCode code_ = 0;
    /// <summary>
    /// Required. The [TypeCode][google.spanner.v1.TypeCode] for this type.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Google.Cloud.Spanner.V1.TypeCode Code {
      get { return code_; }
      set {
        code_ = value;
      }
    }

    /// <summary>Field number for the "array_element_type" field.</summary>
    public const int ArrayElementTypeFieldNumber = 2;
    private global::Google.Cloud.Spanner.V1.Type arrayElementType_;
    /// <summary>
    /// If [code][google.spanner.v1.Type.code] == [ARRAY][google.spanner.v1.TypeCode.ARRAY], then `array_element_type`
    /// is the type of the array elements.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Google.Cloud.Spanner.V1.Type ArrayElementType {
      get { return arrayElementType_; }
      set {
        arrayElementType_ = value;
      }
    }

    /// <summary>Field number for the "struct_type" field.</summary>
    public const int StructTypeFieldNumber = 3;
    private global::Google.Cloud.Spanner.V1.StructType structType_;
    /// <summary>
    /// If [code][google.spanner.v1.Type.code] == [STRUCT][google.spanner.v1.TypeCode.STRUCT], then `struct_type`
    /// provides type information for the struct's fields.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Google.Cloud.Spanner.V1.StructType StructType {
      get { return structType_; }
      set {
        structType_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as Type);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(Type other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Code != other.Code) return false;
      if (!object.Equals(ArrayElementType, other.ArrayElementType)) return false;
      if (!object.Equals(StructType, other.StructType)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Code != 0) hash ^= Code.GetHashCode();
      if (arrayElementType_ != null) hash ^= ArrayElementType.GetHashCode();
      if (structType_ != null) hash ^= StructType.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Code != 0) {
        output.WriteRawTag(8);
        output.WriteEnum((int) Code);
      }
      if (arrayElementType_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(ArrayElementType);
      }
      if (structType_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(StructType);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Code != 0) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Code);
      }
      if (arrayElementType_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(ArrayElementType);
      }
      if (structType_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(StructType);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(Type other) {
      if (other == null) {
        return;
      }
      if (other.Code != 0) {
        Code = other.Code;
      }
      if (other.arrayElementType_ != null) {
        if (arrayElementType_ == null) {
          ArrayElementType = new global::Google.Cloud.Spanner.V1.Type();
        }
        ArrayElementType.MergeFrom(other.ArrayElementType);
      }
      if (other.structType_ != null) {
        if (structType_ == null) {
          StructType = new global::Google.Cloud.Spanner.V1.StructType();
        }
        StructType.MergeFrom(other.StructType);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Code = (global::Google.Cloud.Spanner.V1.TypeCode) input.ReadEnum();
            break;
          }
          case 18: {
            if (arrayElementType_ == null) {
              ArrayElementType = new global::Google.Cloud.Spanner.V1.Type();
            }
            input.ReadMessage(ArrayElementType);
            break;
          }
          case 26: {
            if (structType_ == null) {
              StructType = new global::Google.Cloud.Spanner.V1.StructType();
            }
            input.ReadMessage(StructType);
            break;
          }
        }
      }
    }

  }

  /// <summary>
  /// `StructType` defines the fields of a [STRUCT][google.spanner.v1.TypeCode.STRUCT] type.
  /// </summary>
  public sealed partial class StructType : pb::IMessage<StructType> {
    private static readonly pb::MessageParser<StructType> _parser = new pb::MessageParser<StructType>(() => new StructType());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<StructType> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Google.Cloud.Spanner.V1.TypeReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StructType() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StructType(StructType other) : this() {
      fields_ = other.fields_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public StructType Clone() {
      return new StructType(this);
    }

    /// <summary>Field number for the "fields" field.</summary>
    public const int FieldsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::Google.Cloud.Spanner.V1.StructType.Types.Field> _repeated_fields_codec
        = pb::FieldCodec.ForMessage(10, global::Google.Cloud.Spanner.V1.StructType.Types.Field.Parser);
    private readonly pbc::RepeatedField<global::Google.Cloud.Spanner.V1.StructType.Types.Field> fields_ = new pbc::RepeatedField<global::Google.Cloud.Spanner.V1.StructType.Types.Field>();
    /// <summary>
    /// The list of fields that make up this struct. Order is
    /// significant, because values of this struct type are represented as
    /// lists, where the order of field values matches the order of
    /// fields in the [StructType][google.spanner.v1.StructType]. In turn, the order of fields
    /// matches the order of columns in a read request, or the order of
    /// fields in the `SELECT` clause of a query.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public pbc::RepeatedField<global::Google.Cloud.Spanner.V1.StructType.Types.Field> Fields {
      get { return fields_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as StructType);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(StructType other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!fields_.Equals(other.fields_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= fields_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      fields_.WriteTo(output, _repeated_fields_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      size += fields_.CalculateSize(_repeated_fields_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(StructType other) {
      if (other == null) {
        return;
      }
      fields_.Add(other.fields_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            fields_.AddEntriesFrom(input, _repeated_fields_codec);
            break;
          }
        }
      }
    }

    #region Nested types
    /// <summary>Container for nested types declared in the StructType message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static partial class Types {
      /// <summary>
      /// Message representing a single field of a struct.
      /// </summary>
      public sealed partial class Field : pb::IMessage<Field> {
        private static readonly pb::MessageParser<Field> _parser = new pb::MessageParser<Field>(() => new Field());
        private pb::UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pb::MessageParser<Field> Parser { get { return _parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static pbr::MessageDescriptor Descriptor {
          get { return global::Google.Cloud.Spanner.V1.StructType.Descriptor.NestedTypes[0]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        pbr::MessageDescriptor pb::IMessage.Descriptor {
          get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public Field() {
          OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public Field(Field other) : this() {
          name_ = other.name_;
          type_ = other.type_ != null ? other.type_.Clone() : null;
          _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public Field Clone() {
          return new Field(this);
        }

        /// <summary>Field number for the "name" field.</summary>
        public const int NameFieldNumber = 1;
        private string name_ = "";
        /// <summary>
        /// The name of the field. For reads, this is the column name. For
        /// SQL queries, it is the column alias (e.g., `"Word"` in the
        /// query `"SELECT 'hello' AS Word"`), or the column name (e.g.,
        /// `"ColName"` in the query `"SELECT ColName FROM Table"`). Some
        /// columns might have an empty name (e.g., !"SELECT
        /// UPPER(ColName)"`). Note that a query result can contain
        /// multiple fields with the same name.
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string Name {
          get { return name_; }
          set {
            name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
          }
        }

        /// <summary>Field number for the "type" field.</summary>
        public const int TypeFieldNumber = 2;
        private global::Google.Cloud.Spanner.V1.Type type_;
        /// <summary>
        /// The type of the field.
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public global::Google.Cloud.Spanner.V1.Type Type {
          get { return type_; }
          set {
            type_ = value;
          }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other) {
          return Equals(other as Field);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(Field other) {
          if (ReferenceEquals(other, null)) {
            return false;
          }
          if (ReferenceEquals(other, this)) {
            return true;
          }
          if (Name != other.Name) return false;
          if (!object.Equals(Type, other.Type)) return false;
          return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode() {
          int hash = 1;
          if (Name.Length != 0) hash ^= Name.GetHashCode();
          if (type_ != null) hash ^= Type.GetHashCode();
          if (_unknownFields != null) {
            hash ^= _unknownFields.GetHashCode();
          }
          return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString() {
          return pb::JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(pb::CodedOutputStream output) {
          if (Name.Length != 0) {
            output.WriteRawTag(10);
            output.WriteString(Name);
          }
          if (type_ != null) {
            output.WriteRawTag(18);
            output.WriteMessage(Type);
          }
          if (_unknownFields != null) {
            _unknownFields.WriteTo(output);
          }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize() {
          int size = 0;
          if (Name.Length != 0) {
            size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
          }
          if (type_ != null) {
            size += 1 + pb::CodedOutputStream.ComputeMessageSize(Type);
          }
          if (_unknownFields != null) {
            size += _unknownFields.CalculateSize();
          }
          return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(Field other) {
          if (other == null) {
            return;
          }
          if (other.Name.Length != 0) {
            Name = other.Name;
          }
          if (other.type_ != null) {
            if (type_ == null) {
              Type = new global::Google.Cloud.Spanner.V1.Type();
            }
            Type.MergeFrom(other.Type);
          }
          _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(pb::CodedInputStream input) {
          uint tag;
          while ((tag = input.ReadTag()) != 0) {
            switch(tag) {
              default:
                _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                break;
              case 10: {
                Name = input.ReadString();
                break;
              }
              case 18: {
                if (type_ == null) {
                  Type = new global::Google.Cloud.Spanner.V1.Type();
                }
                input.ReadMessage(Type);
                break;
              }
            }
          }
        }

      }

    }
    #endregion

  }

  #endregion

}

#endregion Designer generated code
