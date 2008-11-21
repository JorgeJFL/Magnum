// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.ProtocolBuffers.Serialization
{
    using System;
    using Common.Reflection;
    using Specs;
    using Streams;

    public class MessageSerializer<TMessage> :
        IMessageSerializer<TMessage> where TMessage : class, new()
    {
        private readonly FieldDescriptors _descriptors = new FieldDescriptors();

        public void Serialize(CodedOutputStream outputStream, object message)
        {
            Serialize(outputStream, (TMessage)message);
        }
        object IMessageSerializer.Deserialize(CodedInputStream inputStream)
        {
            return Deserialize(inputStream);
        }
        public void Serialize(CodedOutputStream outputStream, TMessage message)
        {
            foreach (FieldSerializer prop in _descriptors.GetAll())
            {
                FieldSerializer prop1 = prop;
                var valueToSerialize = prop.Func.Get(message);
                prop.Strategy.Serialize(outputStream, prop1.FieldTag, valueToSerialize);
            }
        }
        public TMessage Deserialize(CodedInputStream inputStream)
        {
            var result = new TMessage();
            var length = inputStream.Length;

            while (inputStream.Position < length)
            {
                TagData tagData = inputStream.ReadTag();
                var field = _descriptors[tagData.NumberTag];
                var deserializedValue = field.Strategy.Deserialize(inputStream);
                field.Func.Set(result, deserializedValue);
            }
            
            return result;
        }

        public bool CanHandle(Type type)
        {
            return MappedType.Equals(type);
        }
        public Type MappedType
        {
            get
            {
                return typeof (TMessage);
            }
        }
        public void AddProperty(int tag, FastProperty fp, Type netType, FieldRules rules, ISerializationStrategy strategy)
        {
            var fd = new FieldSerializer
                         {
                             FieldTag = tag,
                             WireType = DetermineWireType(netType),
                             Func = fp,
                             NetType = netType,
                             Rules = rules,
                             Strategy = strategy
                         };
            _descriptors.Add(fd);
        }

        //todo: this is nasty
        private static WireType DetermineWireType(Type type)
        {
            if (type.IsEnum)
                return WireType.Varint;

            if (typeof(DateTime).Equals(type)) //uint64
                return WireType.Varint;

            if (typeof(Guid).Equals(type)) //two uint64
                return WireType.LengthDelimited;

            if (typeof(int).Equals(type) || typeof(long).Equals(type))
                return WireType.Varint;


            //string, classes
            return WireType.LengthDelimited;
        }
    }
}